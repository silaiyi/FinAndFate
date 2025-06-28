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
    
    [Header("Debug Settings")]
    public bool drawPathGizmos = true; // 新增：控制是否繪製路徑
    public Color pathColor = Color.cyan; // 新增：路徑顏色
    
    [Header("Patrol Settings")]
    public bool pingPongMovement = true; // 新增：啟用往返巡邏模式
    private int patrolDirection = 1; // 新增：巡邏方向 (1=正向, -1=反向)


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
        patrolDirection = 1; // 初始為正向移動

        Debug.Log("Predator initialized. Starting patrol.");
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.ApplyOutline(gameObject, OutlineManager.Instance.predatorOutlineColor);
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅
        SwimmingController.OnPollutionChanged -= HandlePollutionChanged;
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.RemoveOutline(gameObject);
        }
    }
    
    // 修改事件處理
    void HandlePollutionChanged(SwimmingController.PollutionScores newScores)
    {
        // 使用污水分數
        currentPollutionFactor = Mathf.Clamp01(newScores.sewage / 10f);
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
                // 新增：检查玩家是否切换了躲藏状态
                if (playerController != null && playerController.isHiding)
                {
                    HandleHidingPlayer();
                }
                else
                {
                    ChasePlayer();
                }
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
            // 根據往返模式更新索引
            if (pingPongMovement)
            {
                // 往返模式
                currentPathIndex += patrolDirection;
                
                // 檢查是否需要改變方向
                if (currentPathIndex >= pathPoints.Count - 1 && patrolDirection == 1)
                {
                    patrolDirection = -1; // 到達終點，轉為反向
                    Debug.Log("Reached end point - reversing direction");
                }
                else if (currentPathIndex <= 0 && patrolDirection == -1)
                {
                    patrolDirection = 1; // 回到起點，轉為正向
                    Debug.Log("Reached start point - reversing direction");
                }
            }
            else
            {
                // 原始循環模式
                currentPathIndex = (currentPathIndex + 1) % pathPoints.Count;
            }
            
            //Debug.Log("Moving to next waypoint: " + currentPathIndex);
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
        // 新增：如果当前正在破坏珊瑚，但玩家切换到了岩石
        if (currentState == PredatorState.Destroying && 
            playerController.currentObstacleType == SwimmingController.ObstacleType.Rock)
        {
            Debug.Log("Player switched to rock while destroying coral - aborting");
            StartReturning();
            return;
        }

        if (playerController.currentObstacleType == SwimmingController.ObstacleType.None)
        {
            return;
        }

        // 根据障碍物类型处理
        switch (playerController.currentObstacleType)
        {
            case SwimmingController.ObstacleType.Coral:
                // 新增：如果已经在破坏珊瑚，不要重复设置
                if (currentState != PredatorState.Destroying)
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

                // 新增：更智能的玩家检测
                bool playerVisible = CheckPlayerAfterDestruction();
                if (playerVisible)
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
    
    // 新增：破坏珊瑚后的玩家检测方法
    bool CheckPlayerAfterDestruction()
    {
        if (playerTransform == null) return false;
        
        // 检查玩家是否还在躲藏
        if (playerController.isHiding) return false;
        
        // 360度全方位检测
        Vector3 toPlayer = playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;
        
        // 距离检测（比追击半径更宽松）
        if (distance > chaseRadius * 1.5f) return false;
        
        // 视线检测
        RaycastHit hit;
        if (Physics.Raycast(transform.position, toPlayer.normalized, out hit, distance))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
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
        if (drawPathGizmos && pathPoints != null && pathPoints.Count > 0)
        {
            Gizmos.color = pathColor;

            // 繪製所有路徑點和連線
            for (int i = 0; i < pathPoints.Count; i++)
            {
                if (pathPoints[i] == null) continue;

                // 繪製路徑點球體
                Gizmos.DrawSphere(pathPoints[i].position, 0.5f);

                // 繪製點與點之間的連線
                if (i < pathPoints.Count - 1 && pathPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                }
                // 繪製首尾閉合連線
                else if (i == pathPoints.Count - 1 && pathPoints[0] != null)
                {
                    Gizmos.DrawLine(pathPoints[i].position, pathPoints[0].position);
                }

                // 標示路徑點編號
                UnityEditor.Handles.Label(pathPoints[i].position + Vector3.up, $"Point {i}");
            }
        }
        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] == null) continue;

            Vector3 nextPos = (i == pathPoints.Count - 1)
                ? pathPoints[0].position
                : pathPoints[i + 1].position;

            Vector3 dir = (nextPos - pathPoints[i].position).normalized;
            if (dir.sqrMagnitude > 0.01f)
            {
                UnityEditor.Handles.ArrowHandleCap(
                    0,
                    pathPoints[i].position,
                    Quaternion.LookRotation(dir),
                    1f,
                    EventType.Repaint
                );
            }
        }
        if (drawPathGizmos)
        {
            DrawPathGizmos();
        }
    }
    
    void DrawPathGizmos()
    {
        if (pathPoints == null || pathPoints.Count == 0) return;
        
        Gizmos.color = pathColor;
        int validPoints = 0;

        // 繪製所有路徑點和連線
        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] == null) continue;
            
            validPoints++;
            Vector3 position = pathPoints[i].position;
            
            // 繪製路徑點球體
            Gizmos.DrawSphere(position, 0.5f);
            
            // 繪製點與點之間的連線
            if (i < pathPoints.Count - 1 && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(position, pathPoints[i + 1].position);
            }
            
            // 在場景視圖中標示路徑點編號
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up, $"Point {i}");
            #endif
        }

        // 如果啟用往返模式，添加方向指示器
        if (pingPongMovement && validPoints > 1)
        {
            // 繪製起點和終點的箭頭
            DrawDirectionArrow(pathPoints[0].position, pathPoints[1].position, Color.green);
            DrawDirectionArrow(pathPoints[pathPoints.Count - 1].position, 
                              pathPoints[pathPoints.Count - 2].position, Color.red);
        }
    }
    
    void DrawDirectionArrow(Vector3 from, Vector3 to, Color color)
    {
        #if UNITY_EDITOR
        if (pathPoints == null || pathPoints.Count < 2) return;
        
        Vector3 direction = (to - from).normalized;
        float arrowSize = 0.5f;
        
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.ArrowHandleCap(0, 
            from + direction * arrowSize, 
            Quaternion.LookRotation(direction), 
            arrowSize * 2, 
            EventType.Repaint);
        #endif
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
    
    private void OnEnable()
    {
        SwimmingController.OnPollutionChanged += HandlePollutionChanged;
        
        // 立即应用当前污染效果
        if (SwimmingController.Instance != null)
        {
            HandlePollutionChanged(SwimmingController.Instance.GetCurrentPollutionScores());
        }
    }

    private void OnDisable()
    {
        SwimmingController.OnPollutionChanged -= HandlePollutionChanged;
    }
}