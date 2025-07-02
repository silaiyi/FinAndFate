/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FishingBoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    public List<Transform> pathPoints = new List<Transform>();
    public float moveSpeed = 75f; // 基础速度提高到75
    public float rotationSpeed = 5f; // 旋转速度提高以适应新速度
    public float waypointThreshold = 2f;
    public float avoidanceRadius = 30f; // 避让半径增大到30
    public float avoidanceForce = 5f; // 避让力度增大到5

    [Header("Fishing Net Settings")]
    public float netRadius = 5f;
    public float netHeight = 10f;
    public float damageInterval = 0.5f;
    public float netOffset = 2.5f;
    public int netCheckSegments = 5;
    
    [Header("Net Model Reference")]
    public GameObject netModel;
    
    [Header("Debug Settings")]
    public bool drawNetGizmos = true;

    private int currentPointIndex = 0;
    private float nextDamageTime;
    private Collider[] hitColliders = new Collider[20];
    private Vector3 lastNetCenter;
    private Vector3 avoidanceVector;
    public float netDepthOffset = 100f;

    void Start()
    {
        if (pathPoints.Count == 0)
        {
            GameObject[] roadObjects = GameObject.FindGameObjectsWithTag("FishRoad");
            pathPoints = roadObjects.Select(go => go.transform).ToList();
            
            if (pathPoints.Count == 0)
            {
                Debug.LogError("No path points assigned and no FishRoad points found!");
                enabled = false;
            }
        }
        
        currentPointIndex = Random.Range(0, pathPoints.Count);
        
        // 添加速度随机变化 (±25)
        moveSpeed += Random.Range(-25f, 25f);
        
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
        
        lastNetCenter = transform.position - transform.forward * netOffset;
        
        if (netModel != null)
        {
            UpdateNetModelPosition();
        }
    }
    
    void UpdateNetModelPosition()
    {
        netModel.transform.localPosition = new Vector3(0, -netDepthOffset, -netOffset);
        netModel.transform.localScale = new Vector3(
            netRadius * 2,
            netHeight,
            netRadius * 2
        );
    }

    void CalculateAvoidance()
    {
        avoidanceVector = Vector3.zero;
        
        if (FishBoatManager.Instance == null) return;
        
        List<Transform> otherBoats = FishBoatManager.Instance.GetAllBoatTransforms();
        otherBoats.Remove(transform);
        
        foreach (Transform boat in otherBoats)
        {
            Vector3 toOther = transform.position - boat.position;
            float distance = toOther.magnitude;
            
            if (distance < avoidanceRadius)
            {
                float force = Mathf.Clamp01(1 - distance / avoidanceRadius) * avoidanceForce;
                avoidanceVector += toOther.normalized * force;
            }
        }
    }

    void MoveAlongPath()
    {
        Vector3 targetPosition = pathPoints[currentPointIndex].position;
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        moveDirection.y = 0;
        
        // 动态旋转速度（基于当前速度）
        float dynamicRotationSpeed = rotationSpeed * (moveSpeed / 75f);
        
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
                dynamicRotationSpeed * Time.deltaTime
            );
        }

        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

        // 动态路径点阈值（基于速度）
        float dynamicThreshold = Mathf.Max(waypointThreshold, moveSpeed * 0.1f);
        
        if (Vector3.Distance(transform.position, targetPosition) < dynamicThreshold)
        {
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
        else if (target.CompareTag("NPCFish"))
        {
            FishNPC fish = target.GetComponent<FishNPC>();
            if (fish != null)
            {
                Destroy(target);
            }
            else
            {
                Destroy(target);
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

        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] == null) continue;

            Gizmos.DrawSphere(pathPoints[i].position, 1f);
            if (i < pathPoints.Count - 1 && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }

        Vector3 netCenter = transform.position - transform.forward * netOffset - Vector3.up * netDepthOffset;

        DrawGizmoCircle(netCenter - Vector3.up * netHeight * 0.5f, netRadius);
        DrawGizmoCircle(netCenter + Vector3.up * netHeight * 0.5f, netRadius);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2 / 8;
            Vector3 point = new Vector3(Mathf.Cos(angle) * netRadius, 0, Mathf.Sin(angle) * netRadius);
            Gizmos.DrawLine(
                netCenter - Vector3.up * netHeight * 0.5f + point,
                netCenter + Vector3.up * netHeight * 0.5f + point
            );
        }

        if (avoidanceVector.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + avoidanceVector * 5f);
        }
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        DrawGizmoCylinder(netCenter, netRadius, netHeight);
    }
    
    void DrawGizmoCylinder(Vector3 center, float radius, float height)
    {
        int segments = 16;
        
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
            
            Gizmos.DrawLine(bottom1, top1);
            Gizmos.DrawLine(bottom1, bottom2);
            Gizmos.DrawLine(top1, top2);
        }
    }
    
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