using UnityEngine;
using System.Collections.Generic;

public class FishingBoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    public List<Transform> pathPoints = new List<Transform>();
    public float moveSpeed = 8f;
    public float rotationSpeed = 2f;
    public float waypointThreshold = 2f;

    [Header("Fishing Net Settings")]
    public float netRadius = 5f;
    public float netHeight = 10f;
    public float damageInterval = 0.5f;
    public float netOffset = 2.5f;
    
    [Header("Net Visual Reference")]
    public FishingNetVisual netVisual;
    
    [Header("Debug Settings")]
    public bool drawNetGizmos = true; // 控制是否绘制调试图形

    private int currentPointIndex = 0;
    private float nextDamageTime;
    private Collider[] hitColliders = new Collider[20];
    private Vector3 lastNetCenter; // 存储最后计算的网中心位置

    void Start()
    {
        if (pathPoints.Count == 0)
        {
            Debug.LogError("No path points assigned to fishing boat!");
            enabled = false;
        }
        
        InitializeNetVisual();
    }

    void Update()
    {
        if (pathPoints.Count == 0) return;

        MoveAlongPath();
        CheckForTargets();
        
        // 更新网中心位置用于调试绘制
        lastNetCenter = transform.position - transform.forward * netOffset;
    }
    
    void InitializeNetVisual()
    {
        if (netVisual != null)
        {
            netVisual.netRadius = netRadius;
            netVisual.netHeight = netHeight;
            netVisual.netOffset = netOffset;
        }
        else
        {
            netVisual = GetComponentInChildren<FishingNetVisual>();
            if (netVisual != null)
            {
                netVisual.netRadius = netRadius;
                netVisual.netHeight = netHeight;
                netVisual.netOffset = netOffset;
            }
        }
    }

    void MoveAlongPath()
    {
        Vector3 targetPosition = pathPoints[currentPointIndex].position;
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        moveDirection.y = 0; // 保持水平移动

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
            currentPointIndex = (currentPointIndex + 1) % pathPoints.Count;
        }
    }

    void CheckForTargets()
    {
        if (Time.time < nextDamageTime) return;

        // 创建渔网检测区域（船后方的锥形区域）
        Vector3 netCenter = transform.position - transform.forward * netOffset;

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            netCenter - Vector3.up * netHeight * 0.5f,
            netCenter + Vector3.up * netHeight * 0.5f,
            netRadius,
            hitColliders
        );

        for (int i = 0; i < hitCount; i++)
        {
            GameObject target = hitColliders[i].gameObject;

            if (target.CompareTag("Player") || target.CompareTag("Trash"))
            {
                DestroyTarget(target);
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
        else
        {
            Destroy(target);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawNetGizmos) return;
        
        Gizmos.color = Color.red;

        // 绘制路径点
        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] == null) continue;

            Gizmos.DrawSphere(pathPoints[i].position, 1f);
            if (i < pathPoints.Count - 1 && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }

        // 绘制渔网范围
        Vector3 netCenter = transform.position - transform.forward * netOffset;
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            netCenter - Vector3.up * netHeight * 0.5f,
            netCenter + Vector3.up * netHeight * 0.5f,
            netRadius,
            hitColliders
        );
        
        // 绘制顶部和底部的圆
        DrawGizmoCircle(netCenter - Vector3.up * netHeight * 0.5f, netRadius);
        DrawGizmoCircle(netCenter + Vector3.up * netHeight * 0.5f, netRadius);
        
        // 绘制侧面连接线
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2 / 8;
            Vector3 point = new Vector3(Mathf.Cos(angle) * netRadius, 0, Mathf.Sin(angle) * netRadius);
            Gizmos.DrawLine(
                netCenter - Vector3.up * netHeight * 0.5f + point,
                netCenter + Vector3.up * netHeight * 0.5f + point
            );
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