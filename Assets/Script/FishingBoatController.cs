using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FishingBoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    public List<Transform> pathPoints = new List<Transform>();
    public float moveSpeed = 8f;
    public float rotationSpeed = 2f;
    public float waypointThreshold = 2f;
    public float avoidanceRadius = 10f; // 避讓其他船的半徑
    public float avoidanceForce = 2f; // 避讓力度

    [Header("Fishing Net Settings")]
    public float netRadius = 5f;
    public float netHeight = 10f;
    public float damageInterval = 0.5f;
    public float netOffset = 2.5f;
    public int netCheckSegments = 5; // 新增：渔网高度方向检测分段数
    
    [Header("Net Model Reference")] // 新增：实体模型引用
    public GameObject netModel; // 拖拽渔网模型到这个字段
    
    [Header("Debug Settings")]
    public bool drawNetGizmos = true; // 控制是否绘制调试图形

    private int currentPointIndex = 0;
    private float nextDamageTime;
    private Collider[] hitColliders = new Collider[20];
    private Vector3 lastNetCenter; // 存储最后计算的网中心位置
    private Vector3 avoidanceVector; // 避讓方向

    void Start()
    {
        if (pathPoints.Count == 0)
        {
            // 從場景中獲取FishRoad點
            GameObject[] roadObjects = GameObject.FindGameObjectsWithTag("FishRoad");
            pathPoints = roadObjects.Select(go => go.transform).ToList();
            
            if (pathPoints.Count == 0)
            {
                Debug.LogError("No path points assigned and no FishRoad points found!");
                enabled = false;
            }
        }
        
        // 隨機選擇起始點
        currentPointIndex = Random.Range(0, pathPoints.Count);
        
        // 初始化渔网模型位置
        if (netModel != null)
        {
            UpdateNetModelPosition();
        }
    }

    void Update()
    {
        if (pathPoints.Count == 0) return;

        CalculateAvoidance();
        MoveAlongPath();
        CheckForTargets();
        
        // 更新網中心位置
        lastNetCenter = transform.position - transform.forward * netOffset;
        
        // 更新渔网模型位置
        if (netModel != null)
        {
            UpdateNetModelPosition();
        }
    }
    
    // 新增：更新渔网模型位置和大小
    void UpdateNetModelPosition()
    {
        // 设置位置（船尾偏移）
        netModel.transform.localPosition = new Vector3(0, 0, -netOffset);
        
        // 设置缩放（根据半径和高度）
        netModel.transform.localScale = new Vector3(
            netRadius * 2,  // 直径=半径*2
            netHeight,
            netRadius * 2
        );
    }

    void CalculateAvoidance()
    {
        avoidanceVector = Vector3.zero;
        
        if (FishBoatManager.Instance == null) return;
        
        List<Transform> otherBoats = FishBoatManager.Instance.GetAllBoatTransforms();
        otherBoats.Remove(transform); // 移除自己
        
        foreach (Transform boat in otherBoats)
        {
            Vector3 toOther = transform.position - boat.position;
            float distance = toOther.magnitude;
            
            if (distance < avoidanceRadius)
            {
                // 距離越近，避讓力度越大
                float force = Mathf.Clamp01(1 - distance / avoidanceRadius) * avoidanceForce;
                avoidanceVector += toOther.normalized * force;
            }
        }
    }

    void MoveAlongPath()
    {
        Vector3 targetPosition = pathPoints[currentPointIndex].position;
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        moveDirection.y = 0; // 保持水平移動
        
        // 應用避讓向量
        if (avoidanceVector.magnitude > 0.1f)
        {
            moveDirection += avoidanceVector.normalized * 0.3f;
            moveDirection.Normalize();
        }

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

        if (Vector3.Distance(transform.position, targetPosition) < waypointThreshold)
        {
            // 隨機選擇下一個點，避免立即返回
            int newIndex;
            do {
                newIndex = Random.Range(0, pathPoints.Count);
            } while (newIndex == currentPointIndex && pathPoints.Count > 1);
            
            currentPointIndex = newIndex;
        }
    }

    void CheckForTargets()
    {
        if (Time.time < nextDamageTime) return;

        // 创建渔网检测区域（精确圆柱体）
        Vector3 netCenter = transform.position - transform.forward * netOffset;
        Vector3 bottom = netCenter - Vector3.up * netHeight * 0.5f;
        Vector3 top = netCenter + Vector3.up * netHeight * 0.5f;

        float stepHeight = netHeight / (netCheckSegments - 1);
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();

        // 沿高度方向分段检测
        for (int i = 0; i < netCheckSegments; i++)
        {
            Vector3 checkPos = bottom + Vector3.up * (i * stepHeight);
            int hitCount = Physics.OverlapSphereNonAlloc(
                checkPos,
                netRadius,
                hitColliders
            );

            for (int j = 0; j < hitCount; j++)
            {
                GameObject target = hitColliders[j].gameObject;

                // 确保不重复处理同一对象
                if (processedTargets.Contains(target)) continue;
                processedTargets.Add(target);

                if (target.CompareTag("Player") || target.CompareTag("Trash"))
                {
                    DestroyTarget(target);
                }
            }
        }

        nextDamageTime = Time.time + damageInterval;
    }
    void DestroyCoralImmediately(GameObject coral)
    {
        Obstacle obstacle = coral.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            // 直接摧毀珊瑚而不播放效果
            Destroy(coral);
            
            // 或者使用珊瑚的摧毀方法（如果希望有效果）
            // obstacle.DestroyObstacle();
        }
        else
        {
            Destroy(coral);
        }
    }

    void DestroyTarget(GameObject target)
    {
        if (target.CompareTag("Player"))
        {
            SwimmingController player = target.GetComponent<SwimmingController>();
            if (player != null)
            {
                player.InstantDeath();
            }
        }
        else
        {
            Destroy(target);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawNetGizmos) return;

        Gizmos.color = Color.red;

        // 繪製路徑點
        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] == null) continue;

            Gizmos.DrawSphere(pathPoints[i].position, 1f);
            if (i < pathPoints.Count - 1 && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }

        // 繪製漁網範圍
        Vector3 netCenter = transform.position - transform.forward * netOffset;

        // 繪製頂部和底部的圓
        DrawGizmoCircle(netCenter - Vector3.up * netHeight * 0.5f, netRadius);
        DrawGizmoCircle(netCenter + Vector3.up * netHeight * 0.5f, netRadius);

        // 繪製側面連接線
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2 / 8;
            Vector3 point = new Vector3(Mathf.Cos(angle) * netRadius, 0, Mathf.Sin(angle) * netRadius);
            Gizmos.DrawLine(
                netCenter - Vector3.up * netHeight * 0.5f + point,
                netCenter + Vector3.up * netHeight * 0.5f + point
            );
        }

        // 繪製避讓向量
        if (avoidanceVector.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + avoidanceVector * 5f);
        }
        Gizmos.color = new Color(0, 1, 1, 0.5f); // 青色半透明
        
        // 绘制光柱的实际圆柱体
        DrawGizmoCylinder(netCenter, netRadius, netHeight);
    }
    void DrawGizmoCylinder(Vector3 center, float radius, float height)
    {
        int segments = 16;
        
        // 绘制侧面
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * Mathf.PI * 2 / segments;
            float angle2 = (i + 1) * Mathf.PI * 2 / segments;
            
            Vector3 bottom1 = center + new Vector3(
                Mathf.Cos(angle1) * radius,
                -height / 2,
                Mathf.Sin(angle1) * radius
            );
            
            Vector3 bottom2 = center + new Vector3(
                Mathf.Cos(angle2) * radius,
                -height / 2,
                Mathf.Sin(angle2) * radius
            );
            
            Vector3 top1 = center + new Vector3(
                Mathf.Cos(angle1) * radius,
                height / 2,
                Mathf.Sin(angle1) * radius
            );
            
            Vector3 top2 = center + new Vector3(
                Mathf.Cos(angle2) * radius,
                height / 2,
                Mathf.Sin(angle2) * radius
            );
            
            // 底部到顶部
            Gizmos.DrawLine(bottom1, top1);
            // 底部环
            Gizmos.DrawLine(bottom1, bottom2);
            // 顶部环
            Gizmos.DrawLine(top1, top2);
        }
    }
    
    // 在Gizmos中绘制圆形
    void DrawGizmoCircle(Vector3 center, float radius)
    {
        int segments = 16;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}