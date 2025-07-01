using UnityEngine;
using System.Collections.Generic;

public class ChasingBoatController : MonoBehaviour
{
    [Header("Chasing Settings")]
    public Transform playerTarget;
    public float moveSpeed = 10f;
    public float rotationSpeed = 5f;
    public float chaseDistance = 30f;
    public float attackDistance = 5f;
    
    [Header("Net Settings")]
    public float netRadius = 5f;
    public float netHeight = 10f;
    public float damageInterval = 0.5f;
    public float netOffset = 2.5f;
    public int netCheckSegments = 5;
    public float netDepthOffset = 100f;
    
    [Header("Net Model Reference")]
    public GameObject netModel;
    
    [Header("Debug Settings")]
    public bool drawNetGizmos = true;
    
    private float nextDamageTime;
    private Collider[] hitColliders = new Collider[20];
    private Vector3 lastNetCenter;
    void Start()
    {
        if (EnemyIndicatorManager.Instance != null)
        {
            EnemyIndicatorManager.Instance.RegisterEnemy(transform);
        }
    }
    void Update()
    {
        if (playerTarget == null)
        {
            // 尝试重新获取玩家引用
            if (SwimmingController.Instance != null)
            {
                playerTarget = SwimmingController.Instance.transform;
            }
            else
            {
                Debug.LogWarning("Player target is null!");
                return;
            }
        }

        // 获取玩家位置但保持高度为0
        Vector3 targetPosition = new Vector3(
            playerTarget.position.x,
            0, // 强制高度为0
            playerTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, targetPosition);

        // 追逐邏輯 - 移除距离限制
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // 确保只在水平面移动

        // 旋轉朝向玩家
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 移動 (只在水平面移动)
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

        // 强制设置高度为0
        Vector3 pos = transform.position;
        pos.y = 0;
        transform.position = pos;

        // 更新網中心位置
        lastNetCenter = transform.position - transform.forward * netOffset - Vector3.up * netDepthOffset;
        
        // 更新渔网模型位置
        if (netModel != null)
        {
            UpdateNetModelPosition();
        }
        
        // 檢查漁網範圍內的目標
        CheckForTargets();
    }
    void OnDestroy()
    {
        if (EnemyIndicatorManager.Instance != null)
        {
            EnemyIndicatorManager.Instance.UnregisterEnemy(transform);
        }
    }
    
    // 更新渔网模型位置和大小
    void UpdateNetModelPosition()
    {
        // 设置位置（船尾偏移）
        netModel.transform.localPosition = new Vector3(0, -netDepthOffset, -netOffset);
        
        // 设置缩放（根据半径和高度）
        netModel.transform.localScale = new Vector3(
            netRadius * 2,  // 直径=半径*2
            netHeight,
            netRadius * 2
        );
    }

    void CheckForTargets()
    {
        if (Time.time < nextDamageTime) return;

        Vector3 netCenter = transform.position - transform.forward * netOffset - Vector3.up * netDepthOffset;
        Vector3 bottom = netCenter - Vector3.up * netHeight * 0.5f;
        Vector3 top = netCenter + Vector3.up * netHeight * 0.5f;

        float stepHeight = netHeight / (netCheckSegments - 1);
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();

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

                if (processedTargets.Contains(target)) continue;
                processedTargets.Add(target);

                // 添加对NPCFish标签的检测
                if (target.CompareTag("Player") || 
                    target.CompareTag("Trash") || 
                    target.CompareTag("NPCFish"))
                {
                    DestroyTarget(target);
                }
            }
        }

        nextDamageTime = Time.time + damageInterval;
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
        // 新增：NPC鱼类秒杀处理
        else if (target.CompareTag("NPCFish"))
        {
            FishNPC fish = target.GetComponent<FishNPC>();
            if (fish != null)
            {
                // 可以在这里添加鱼死亡效果
                Destroy(target);
            }
            else
            {
                Destroy(target);
            }
        }
        else // 垃圾和其他对象
        {
            Destroy(target);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawNetGizmos) return;

        Gizmos.color = Color.red;

        // 繪製漁網範圍
        Vector3 netCenter = transform.position - transform.forward * netOffset - Vector3.up * netDepthOffset;

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