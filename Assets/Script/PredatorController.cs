using UnityEngine;
using System.Collections.Generic;

public class PredatorController : MonoBehaviour
{
    public enum PredatorState { Patrolling, Chasing, Destroying, Returning }

    [Header("Movement Settings")]
    public List<Transform> pathPoints = new List<Transform>();
    public float patrolSpeed = 6f;
    public float chaseSpeed = 5.5f;
    public float rotationSpeed = 3f;
    public float waypointThreshold = 2f;
    public float verticalChaseFactor = 0.7f;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public float chaseRadius = 40f;
    public float maxVerticalAngle = 70f;
    public float maxHorizontalAngle = 90f;
    public float visionCheckInterval = 0.2f;

    [Header("Obstacle Settings")]
    public float destroyTime = 2f;
    public float searchDuration = 5f;

    [Header("Pollution Settings")]
    public float baseDetectionRadius = 200f;
    public float baseChaseRadius = 200f;
    public float baseChaseSpeed = 9f;
    public float baseHorizontalAngle = 90f;
    public float baseVerticalAngle = 70f;
    public float maxChaseSpeed = 19f;

    [Header("Debug Settings")]
    public bool drawPathGizmos = true;
    public Color pathColor = Color.cyan;
    
    [Header("Patrol Settings")]
    public bool pingPongMovement = true;
    
    [Header("NPC Fish Settings")]
    public float npcDetectionRadius = 20f; // NPC检测半径
    public float npcPriorityMultiplier = 1.5f; // NPC优先级乘数
    
    [Header("Health & Poison Settings")]
    public int maxHealth = 3000; // 最大生命值
    public float poisonDuration = 10f; // 中毒持续时间
    public float poisonDamageInterval = 1f; // 中毒伤害间隔
    public float poisonedSpeedMultiplier = 0.5f; // 中毒时速度乘数

    // 状态变量
    private PredatorState currentState;
    private int currentPathIndex = 0;
    private Transform playerTransform;
    private SwimmingController playerController;
    private GameObject targetObstacle;
    private float stateTimer;
    private float nextVisionCheck;
    private Vector3 lastKnownPlayerPosition;
    private float returnToPathThreshold = 1f;
    private int patrolDirection = 1;
    private float currentPollutionFactor = 0f;
    private Rigidbody rb;
    
    // NPC鱼追踪
    private Transform currentTarget; // 当前追踪目标（玩家或NPC）
    private bool hasNPCTarget = false; // 是否有NPC目标
    
    // 中毒系统
    private bool isPoisoned = false;
    private float poisonTimer = 0f;
    private float poisonDamageTimer = 0f;
    private int poisonDamagePerSecond = 0; // 当前中毒伤害值
    private int poisonStacks = 0; // 中毒层数
    private int currentHealth; // 当前生命值
    private float originalChaseSpeed; // 原始追击速度
    private float originalPatrolSpeed; // 原始巡逻速度

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
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
        }

        SwimmingController.OnPollutionChanged += HandlePollutionChanged;
        patrolDirection = 1;

        // 初始化生命值
        currentHealth = maxHealth;

        // 保存原始速度
        originalChaseSpeed = chaseSpeed;
        originalPatrolSpeed = patrolSpeed;
        if (EnemyIndicatorManager.Instance != null)
        {
            EnemyIndicatorManager.Instance.RegisterEnemy(transform);
        }
    }

    void OnDestroy()
    {
        SwimmingController.OnPollutionChanged -= HandlePollutionChanged;
        if (EnemyIndicatorManager.Instance != null)
        {
            EnemyIndicatorManager.Instance.UnregisterEnemy(transform);
        }
    }
    
    void HandlePollutionChanged(SwimmingController.PollutionScores newScores)
    {
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
        
        // 中毒时速度额外减少
        if (isPoisoned)
        {
            chaseSpeed = originalChaseSpeed * poisonedSpeedMultiplier;
            patrolSpeed = originalPatrolSpeed * poisonedSpeedMultiplier;
        }
    }
    
    void Update()
    {
        // 处理中毒状态
        HandlePoisonEffect();
        
        // 优先检测NPC鱼
        CheckForNPCFish();
        
        switch (currentState)
        {
            case PredatorState.Patrolling:
                Patrol();
                if (!hasNPCTarget) CheckForPlayer();
                break;

            case PredatorState.Chasing:
                if (hasNPCTarget && currentTarget == null)
                {
                    // NPC目标已消失
                    hasNPCTarget = false;
                    currentState = PredatorState.Patrolling;
                    break;
                }
                
                if (playerController != null && playerController.isHiding && !hasNPCTarget)
                {
                    HandleHidingPlayer();
                }
                else
                {
                    ChaseTarget();
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
    
    void HandlePoisonEffect()
    {
        if (isPoisoned)
        {
            // 更新中毒计时器
            poisonTimer -= Time.deltaTime;
            poisonDamageTimer += Time.deltaTime;
            
            // 应用速度惩罚
            chaseSpeed = originalChaseSpeed * poisonedSpeedMultiplier;
            patrolSpeed = originalPatrolSpeed * poisonedSpeedMultiplier;
            
            // 每秒造成伤害
            if (poisonDamageTimer >= poisonDamageInterval)
            {
                currentHealth -= poisonDamagePerSecond;
                poisonDamageTimer = 0f;
                
                // 死亡检查
                if (currentHealth <= 0)
                {
                    Die();
                    return;
                }
            }
            
            // 中毒状态结束
            if (poisonTimer <= 0)
            {
                isPoisoned = false;
                chaseSpeed = originalChaseSpeed;
                patrolSpeed = originalPatrolSpeed;
                Debug.Log("Poison effect ended. Total poison stacks: " + poisonStacks);
            }
        }
    }
    
    void ApplyPoison(int damage)
    {
        // 增加中毒层数和伤害
        poisonStacks++;
        poisonDamagePerSecond += damage;
        
        // 重置中毒计时器
        poisonTimer = poisonDuration;
        poisonDamageTimer = 0f;
        isPoisoned = true;
        
        Debug.Log($"Poison applied! Stacks: {poisonStacks}, DPS: {poisonDamagePerSecond}");
    }
    
    void Die()
    {
        Debug.Log("Predator died from poison!");
        Destroy(gameObject);
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
            if (pingPongMovement)
            {
                currentPathIndex += patrolDirection;
                
                if (currentPathIndex >= pathPoints.Count - 1 && patrolDirection == 1)
                {
                    patrolDirection = -1;
                }
                else if (currentPathIndex <= 0 && patrolDirection == -1)
                {
                    patrolDirection = 1;
                }
            }
            else
            {
                currentPathIndex = (currentPathIndex + 1) % pathPoints.Count;
            }
        }
    }
    
    void CheckForNPCFish()
    {
        if (Time.time < nextVisionCheck) return;
        nextVisionCheck = Time.time + visionCheckInterval;
        
        // 查找附近的NPC鱼
        Collider[] nearbyFish = Physics.OverlapSphere(transform.position, npcDetectionRadius);
        Transform closestNPCTarget = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider fish in nearbyFish)
        {
            if (fish != null && fish.CompareTag("NPCFish"))
            {
                float distance = Vector3.Distance(transform.position, fish.transform.position);
                
                // 检查距离
                if (distance > npcDetectionRadius) continue;
                
                Vector3 directionToFish = (fish.transform.position - transform.position).normalized;
                
                // 计算水平角度
                float horizontalAngle = Vector3.Angle(transform.forward, directionToFish);
                
                // 计算垂直角度
                Vector3 horizontalDirection = new Vector3(directionToFish.x, 0, directionToFish.z).normalized;
                float verticalAngle = Vector3.Angle(horizontalDirection, directionToFish);
                
                // 检查是否在视野范围内
                if (horizontalAngle > maxHorizontalAngle / 2 || verticalAngle > maxVerticalAngle / 2) continue;
                
                // 视线检测
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToFish, out hit, npcDetectionRadius))
                {
                    if (hit.collider.CompareTag("NPCFish"))
                    {
                        // 找到最近的NPC鱼
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestNPCTarget = fish.transform;
                        }
                    }
                }
            }
        }
        
        // 如果有找到NPC鱼
        if (closestNPCTarget != null)
        {
            currentTarget = closestNPCTarget;
            hasNPCTarget = true;
            currentState = PredatorState.Chasing;
            lastKnownPlayerPosition = closestNPCTarget.position;
            Debug.Log("NPC Fish detected! Chasing target.");
        }
        else
        {
            // 没有NPC目标时清除标记
            hasNPCTarget = false;
        }
    }

    void CheckForPlayer()
    {
        if (Time.time < nextVisionCheck || hasNPCTarget) return;
        nextVisionCheck = Time.time + visionCheckInterval;

        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 检查距离
        if (distanceToPlayer > detectionRadius) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;

        // 计算水平角度
        float horizontalAngle = Vector3.Angle(transform.forward, directionToPlayer);

        // 计算垂直角度
        Vector3 horizontalDirection = new Vector3(directionToPlayer.x, 0, directionToPlayer.z).normalized;
        float verticalAngle = Vector3.Angle(horizontalDirection, directionToPlayer);

        // 检查是否在视野范围内
        if (horizontalAngle > maxHorizontalAngle / 2 || verticalAngle > maxVerticalAngle / 2) return;

        // 视线检测
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRadius))
        {
            if (hit.collider.CompareTag("Player"))
            {
                currentTarget = playerTransform;
                hasNPCTarget = false;
                currentState = PredatorState.Chasing;
                lastKnownPlayerPosition = playerTransform.position;
            }
        }
    }

    void ChaseTarget()
    {
        if (currentTarget == null)
        {
            StartReturning();
            return;
        }

        // 更新最后已知位置
        if (!hasNPCTarget && playerController != null && !playerController.isHiding)
        {
            lastKnownPlayerPosition = currentTarget.position;
        }
        else if (hasNPCTarget)
        {
            lastKnownPlayerPosition = currentTarget.position;
        }

        // 计算到目标的方向
        Vector3 toTarget = lastKnownPlayerPosition - transform.position;
        float distance = toTarget.magnitude;

        // 检查是否超出追击范围
        if (distance > chaseRadius && !hasNPCTarget)
        {
            StartReturning();
            return;
        }

        // 计算追击方向
        Vector3 moveDirection = toTarget.normalized;

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

        // 检查玩家是否躲在障碍物后
        if (!hasNPCTarget && playerController != null && playerController.isHiding)
        {
            HandleHidingPlayer();
        }
    }

    void HandleHidingPlayer()
    {
        if (currentState == PredatorState.Destroying && 
            playerController.currentObstacleType == SwimmingController.ObstacleType.Rock)
        {
            StartReturning();
            return;
        }

        if (playerController.currentObstacleType == SwimmingController.ObstacleType.None)
        {
            return;
        }

        switch (playerController.currentObstacleType)
        {
            case SwimmingController.ObstacleType.Coral:
                if (currentState != PredatorState.Destroying)
                {
                    targetObstacle = playerController.currentHideObstacle;
                    if (targetObstacle != null)
                    {
                        currentState = PredatorState.Destroying;
                        stateTimer = 0f;
                    }
                }
                break;

            case SwimmingController.ObstacleType.Rock:
                StartReturning();
                break;
        }
    }

    void DestroyObstacle()
    {
        if (targetObstacle == null)
        {
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

            if (stateTimer >= destroyTime)
            {
                Obstacle obstacle = targetObstacle.GetComponent<Obstacle>();
                if (obstacle != null)
                {
                    obstacle.DestroyObstacle();
                }

                bool playerVisible = CheckPlayerAfterDestruction();
                if (playerVisible)
                {
                    currentState = PredatorState.Chasing;
                }
                else
                {
                    StartReturning();
                }

                targetObstacle = null;
            }
        }
    }
    
    bool CheckPlayerAfterDestruction()
    {
        if (playerTransform == null) return false;
        if (playerController.isHiding) return false;
        
        Vector3 toPlayer = playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > chaseRadius * 1.5f) return false;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, toPlayer.normalized, out hit, distance))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }

    void StartReturning()
    {
        currentState = PredatorState.Returning;
        stateTimer = 0f;
        targetObstacle = null;
        currentTarget = null;
        hasNPCTarget = false;
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
                currentState = PredatorState.Patrolling;
            }
        }

        // 搜索时间结束后继续巡逻
        if (stateTimer >= searchDuration)
        {
            currentState = PredatorState.Patrolling;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        HandleTargetCollision(collision.gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        HandleTargetCollision(other.gameObject);
    }
    
    void HandleTargetCollision(GameObject obj)
    {
        // 处理与NPC鱼的碰撞
        if (obj.CompareTag("NPCFish"))
        {
            FishNPC fishNPC = obj.GetComponent<FishNPC>();
            if (fishNPC != null)
            {
                // 检查是否是毒鱼
                if (fishNPC.isToxic)
                {
                    ApplyPoison(1); // 每次中毒增加1点DPS
                }
                
                // 销毁NPC鱼
                Destroy(obj);
                Debug.Log("NPC Fish destroyed!");
                
                // 清除当前目标
                currentTarget = null;
                hasNPCTarget = false;
                
                // 返回巡逻状态
                currentState = PredatorState.Patrolling;
            }
        }
        // 处理与玩家的碰撞
        else if (obj.CompareTag("Player"))
        {
            SwimmingController player = obj.GetComponent<SwimmingController>();
            if (player != null)
            {
                player.InstantDeath();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // 绘制NPC检测范围
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, npcDetectionRadius);
        
        // 绘制检测范围球体
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        // 绘制追击范围
        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawSphere(transform.position, chaseRadius);

        // 绘制视野锥形
        DrawVisionCone();
        
        // 绘制路径
        if (drawPathGizmos && pathPoints != null && pathPoints.Count > 0)
        {
            Gizmos.color = pathColor;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                if (pathPoints[i] == null) continue;
                
                Gizmos.DrawSphere(pathPoints[i].position, 0.5f);
                
                if (i < pathPoints.Count - 1 && pathPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                }
                else if (i == pathPoints.Count - 1 && pathPoints[0] != null)
                {
                    Gizmos.DrawLine(pathPoints[i].position, pathPoints[0].position);
                }
            }
        }
    }

    void DrawVisionCone()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, -maxHorizontalAngle / 2, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, maxHorizontalAngle / 2, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir * detectionRadius);
        Gizmos.DrawRay(transform.position, rightDir * detectionRadius);

        Vector3 upDir = Quaternion.Euler(-maxVerticalAngle / 2, 0, 0) * transform.forward;
        Vector3 downDir = Quaternion.Euler(maxVerticalAngle / 2, 0, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, upDir * detectionRadius);
        Gizmos.DrawRay(transform.position, downDir * detectionRadius);
    }
    
    private void OnEnable()
    {
        SwimmingController.OnPollutionChanged += HandlePollutionChanged;
        
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