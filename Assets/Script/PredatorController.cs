using UnityEngine;
using System.Collections.Generic;

public class PredatorController : MonoBehaviour
{
    public enum PredatorState { Patrolling, Chasing, Destroying, Returning }

    [Header("Movement Settings")]
    public List<Transform> pathPoints = new List<Transform>();
    public float patrolSpeed = 6f;
    public float chaseSpeed = 5.5f; // 比玩家快0.5
    public float rotationSpeed = 3f;
    public float waypointThreshold = 2f;
    public float verticalChaseFactor = 0.7f;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public float chaseRadius = 40f; // 增大追击半径
    public float maxVerticalAngle = 70f;
    public float maxHorizontalAngle = 90f;
    public float visionCheckInterval = 0.2f;

    [Header("Obstacle Settings")]
    public float destroyTime = 2f;
    public float searchDuration = 5f;

    private PredatorState currentState;
    private int currentPathIndex = 0;
    private Transform playerTransform;
    private SwimmingController playerController;
    private GameObject targetObstacle;
    private float stateTimer;
    private float nextVisionCheck;
    private Vector3 lastKnownPlayerPosition;
    private float returnToPathThreshold = 1f;
    [Header("Pollution Settings")]
    public float baseDetectionRadius = 200f;
    public float baseChaseRadius = 200f;
    public float baseChaseSpeed = 9f;
    public float baseHorizontalAngle = 90f;
    public float baseVerticalAngle = 70f;
    public float maxChaseSpeed = 19f; // 污染满级时最大速度

    private float currentPollutionFactor = 0f;
    private Rigidbody rb;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player not found! Make sure there's a GameObject with 'Player' tag.");
            enabled = false;
            return;
        }

        playerController = playerTransform.GetComponent<SwimmingController>();
        if (playerController == null)
        {
            Debug.LogError("SwimmingController not found on player!");
            enabled = false;
            return;
        }

        currentState = PredatorState.Patrolling;
        Debug.Log("Predator initialized. Starting patrol.");
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            Debug.Log("Added Rigidbody to predator");
        }
        SwimmingController.OnPollutionChanged += HandlePollutionChanged;
    }
    void OnDestroy()
    {
        // 取消订阅
        SwimmingController.OnPollutionChanged -= HandlePollutionChanged;
    }
    void HandlePollutionChanged(int newScore)
    {
        currentPollutionFactor = Mathf.Clamp01(newScore / 10f);
        ApplyPollutionEffects();
    }
    void ApplyPollutionEffects()
    {
        detectionRadius = Mathf.Lerp(baseDetectionRadius, baseDetectionRadius * 0.5f, currentPollutionFactor);
        chaseRadius = Mathf.Lerp(baseChaseRadius, baseChaseRadius * 0.5f, currentPollutionFactor);
        maxHorizontalAngle = Mathf.Lerp(baseHorizontalAngle, baseHorizontalAngle * 0.5f, currentPollutionFactor);
        maxVerticalAngle = Mathf.Lerp(baseVerticalAngle, baseVerticalAngle * 0.5f, currentPollutionFactor);
        chaseSpeed = Mathf.Lerp(baseChaseSpeed, maxChaseSpeed, currentPollutionFactor);
        
        Debug.Log($"Pollution updated: {currentPollutionFactor}. New speed: {chaseSpeed}");
    }
    void Update()
    {
        switch (currentState)
        {
            case PredatorState.Patrolling:
                Patrol();
                CheckForPlayer();
                break;

            case PredatorState.Chasing:
                ChasePlayer();
                break;

            case PredatorState.Destroying:
                DestroyObstacle();
                break;

            case PredatorState.Returning:
                ReturnToPatrol();
                break;
        }
    }

    void Patrol()
    {
        if (pathPoints.Count == 0) return;

        Vector3 targetPosition = pathPoints[currentPathIndex].position;
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        transform.Translate(Vector3.forward * patrolSpeed * Time.deltaTime, Space.Self);

        if (Vector3.Distance(transform.position, targetPosition) < waypointThreshold)
        {
            currentPathIndex = (currentPathIndex + 1) % pathPoints.Count;
            Debug.Log("Moving to next waypoint: " + currentPathIndex);
        }
    }

    void CheckForPlayer()
    {
        if (Time.time < nextVisionCheck) return;
        nextVisionCheck = Time.time + visionCheckInterval;

        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 检查距离
        if (distanceToPlayer > detectionRadius)
        {
            Debug.DrawLine(transform.position, playerTransform.position, Color.gray);
            return;
        }

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;

        // 计算水平角度
        float horizontalAngle = Vector3.Angle(transform.forward, directionToPlayer);

        // 计算垂直角度（相对于水平面）
        Vector3 horizontalDirection = new Vector3(directionToPlayer.x, 0, directionToPlayer.z).normalized;
        float verticalAngle = Vector3.Angle(horizontalDirection, directionToPlayer);

        // 检查是否在视野范围内
        if (horizontalAngle > maxHorizontalAngle / 2 || verticalAngle > maxVerticalAngle / 2)
        {
            Debug.DrawLine(transform.position, playerTransform.position, Color.blue);
            return;
        }

        // 视线检测（无遮挡）
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRadius))
        {
            Debug.DrawLine(transform.position, hit.point, Color.red, 1f);
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Player detected!");
                currentState = PredatorState.Chasing;
                lastKnownPlayerPosition = playerTransform.position; // 设置最后已知位置
            }
        }
        else
        {
            Debug.DrawLine(transform.position, transform.position + directionToPlayer * detectionRadius, Color.green);
        }
    }

    void ChasePlayer()
    {
        if (playerTransform == null)
        {
            StartReturning();
            return;
        }

        // 更新最后已知位置
        if (!playerController.isHiding)
        {
            lastKnownPlayerPosition = playerTransform.position;
        }

        // 计算到玩家的方向（三维空间）
        Vector3 toPlayer = lastKnownPlayerPosition - transform.position;
        float distance = toPlayer.magnitude;

        // 检查是否超出追击范围
        if (distance > chaseRadius)
        {
            Debug.Log($"Player out of chase range: {distance} > {chaseRadius}");
            StartReturning();
            return;
        }

        // 计算追击方向
        Vector3 moveDirection = toPlayer.normalized;

        // 调整垂直方向的追击强度
        Vector3 adjustedDirection = new Vector3(
            moveDirection.x,
            moveDirection.y * verticalChaseFactor,
            moveDirection.z
        );

        // 确保方向不为零向量
        if (adjustedDirection.sqrMagnitude > 0.001f)
        {
            adjustedDirection.Normalize();

            // 三维空间中的旋转
            Quaternion targetRotation = Quaternion.LookRotation(adjustedDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // 三维移动
            transform.Translate(Vector3.forward * chaseSpeed * Time.deltaTime, Space.Self);
        }

        Debug.DrawLine(transform.position, lastKnownPlayerPosition, Color.magenta, 0.1f);

        // 检查玩家是否躲在障碍物后
        if (playerController.isHiding)
        {
            Debug.Log("Player is hiding");
            HandleHidingPlayer();
        }
    }

    void HandleHidingPlayer()
    {
        if (playerController.currentObstacleType == SwimmingController.ObstacleType.None)
        {
            return;
        }

        // 根据障碍物类型处理
        switch (playerController.currentObstacleType)
        {
            case SwimmingController.ObstacleType.Coral:
                if (targetObstacle == null)
                {
                    targetObstacle = playerController.currentHideObstacle;
                    if (targetObstacle != null)
                    {
                        currentState = PredatorState.Destroying;
                        stateTimer = 0f;
                        Debug.Log("Targeting coral obstacle");
                    }
                }
                break;

            case SwimmingController.ObstacleType.Rock:
                Debug.Log("Player hiding in rock - returning to patrol");
                StartReturning();
                break;
        }
    }

    void DestroyObstacle()
    {
        if (targetObstacle == null)
        {
            Debug.Log("Target obstacle missing - returning");
            StartReturning();
            return;
        }

        // 移动到障碍物位置
        Vector3 directionToObstacle = (targetObstacle.transform.position - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(directionToObstacle);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        transform.Translate(Vector3.forward * patrolSpeed * Time.deltaTime, Space.Self);

        // 到达障碍物后开始破坏
        float distance = Vector3.Distance(transform.position, targetObstacle.transform.position);
        if (distance < 3f)
        {
            stateTimer += Time.deltaTime;
            Debug.Log($"Destroying obstacle: {stateTimer}/{destroyTime}");

            if (stateTimer >= destroyTime)
            {
                Obstacle obstacle = targetObstacle.GetComponent<Obstacle>();
                if (obstacle != null)
                {
                    obstacle.DestroyObstacle();
                }

                // 破坏后检查玩家是否可见
                if (IsPlayerVisible())
                {
                    Debug.Log("Player visible after destruction - chasing");
                    currentState = PredatorState.Chasing;
                }
                else
                {
                    Debug.Log("Player not visible - returning");
                    StartReturning();
                }

                targetObstacle = null;
            }
        }
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer < 3f) // 如果玩家在破坏范围内
        {
            playerController.InstantDeath();
        }
    }

    void StartReturning()
    {
        Debug.Log("Starting return to patrol");
        currentState = PredatorState.Returning;
        stateTimer = 0f;
        targetObstacle = null;
    }

    void ReturnToPatrol()
    {
        stateTimer += Time.deltaTime;

        // 寻找最近的路径点
        if (pathPoints.Count > 0)
        {
            float minDistance = float.MaxValue;
            int nearestIndex = 0;

            for (int i = 0; i < pathPoints.Count; i++)
            {
                if (pathPoints[i] == null) continue;

                float distance = Vector3.Distance(transform.position, pathPoints[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            currentPathIndex = nearestIndex;

            // 向路径点移动
            Vector3 moveDirection = (pathPoints[currentPathIndex].position - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            transform.Translate(Vector3.forward * patrolSpeed * Time.deltaTime, Space.Self);

            // 接近路径点后继续巡逻
            if (Vector3.Distance(transform.position, pathPoints[currentPathIndex].position) < returnToPathThreshold)
            {
                Debug.Log("Returned to path - resuming patrol");
                currentState = PredatorState.Patrolling;
            }
        }

        // 搜索时间结束后继续巡逻
        if (stateTimer >= searchDuration)
        {
            Debug.Log("Search time ended - resuming patrol");
            currentState = PredatorState.Patrolling;
        }
    }

    bool IsPlayerVisible()
    {
        if (playerTransform == null) return false;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, distance))
        {
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        // 绘制检测范围球体
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        // 绘制追击范围
        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawSphere(transform.position, chaseRadius);

        // 绘制视野锥形
        DrawVisionCone();
    }

    void DrawVisionCone()
    {
        Gizmos.color = Color.yellow;

        // 水平视野
        Vector3 leftDir = Quaternion.Euler(0, -maxHorizontalAngle / 2, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, maxHorizontalAngle / 2, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir * detectionRadius);
        Gizmos.DrawRay(transform.position, rightDir * detectionRadius);

        // 垂直视野
        Vector3 upDir = Quaternion.Euler(-maxVerticalAngle / 2, 0, 0) * transform.forward;
        Vector3 downDir = Quaternion.Euler(maxVerticalAngle / 2, 0, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, upDir * detectionRadius);
        Gizmos.DrawRay(transform.position, downDir * detectionRadius);
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SwimmingController player = collision.gameObject.GetComponent<SwimmingController>();
            if (player != null)
            {
                player.InstantDeath();
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger with {other.gameObject.name}");
        HandlePlayerCollision(other.gameObject);
    }
    void HandlePlayerCollision(GameObject obj)
    {
        if (obj.CompareTag("Player"))
        {
            Debug.Log("Player collision/trigger detected. Killing player.");
            SwimmingController player = obj.GetComponent<SwimmingController>();
            if (player != null)
            {
                player.InstantDeath();
            }
        }
    }
}