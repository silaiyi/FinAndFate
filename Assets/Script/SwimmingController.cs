using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SwimmingController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 120.0f;
    public float pitchSpeed = 60.0f;

    [Header("Camera Settings")]
    public Transform cameraTarget;
    public float cameraDistance = 5.0f;
    public float cameraHeight = 2.0f;
    public float rotationSmoothness = 0.1f;

    [Header("Trash Generation Settings")]
    public GameObject[] level1TrashPrefabs;  // 低危险垃圾
    public GameObject[] level2TrashPrefabs;
    public GameObject[] level3TrashPrefabs;
    public GameObject[] level4TrashPrefabs;  // 高危险垃圾
    public GameObject[] level5TrashPrefabs;
    public float minSpawnInterval = 3.0f;
    public float maxSpawnInterval = 10.0f;
    public int maxTrashInView = 20;
    public float spawnDistanceMin = 10f;
    public float spawnDistanceMax = 30f;
    public float spawnAngle = 60f;
    [Range(0.1f, 1.0f)] public float dangerLevelBias = 0.5f;
    [Range(0, 10)]
    public int score = 10;

    [Header("Player Health Settings")]
    public int initialMaxHealth = 100, seecurrentMaxHp; // 改为初始最大生命值
    public int currentHealth;
    private int currentMaxHealth; // 实际最大生命值
    private Vector3 knockbackVelocity;
    private Vector3 currentForwardDirection;
    private Quaternion targetCameraRotation;
    private Vector3 cameraVelocity;
    private List<GameObject> activeTrash = new List<GameObject>();
    private float nextSpawnTime;
    private Camera mainCamera;
    [Header("Health UI Settings")]
    public Slider healthSlider;               // 生命值滑条
    public Text healthText;                   // 生命值文本
    public Color normalTextColor = Color.white; // 正常文本颜色
    public Color lowHealthColor = Color.red;   // 低生命值文本颜色
    public float smoothSpeed = 25f;           // 滑条平滑过渡速度
    public float blinkInterval = 0.5f;        // 闪烁间隔时间
    public int lowHealthThreshold = 40;       // 低生命值阈值

    // UI相关私有变量
    private bool isLowHealth = false;         // 是否低生命值状态
    private Coroutine blinkCoroutine;         // 闪烁协程引用
    private Image sliderFillImage;            // 滑条填充图像
    private Color originalFillColor;          // 原始填充颜色

    // 新添加的变量用于解决UI不同步问题
    private float lastHealthUpdateTime;       // 上次血量更新时间
    private const float UI_UPDATE_DELAY = 0.05f; // UI更新延迟
    private float targetSliderValue;          // 滑条目标值
    [Header("Pollution Progression")]
    public float pollutionIncreaseRate = 1.0f; // 基础增长率（每分钟）
    public float pollutionAcceleration = 0.2f; // 污染加速因子
    public float pollutionUpdateInterval = 10f; // 更新间隔（秒）
    private float pollutionTimer;
    private float currentPollution;
    [Header("Trash Generation Settings")]
    // 增加高危害垃圾生成权重
    [Range(0.5f, 2.0f)] public float highDangerBias = 1.2f;
    public delegate void PollutionChangedHandler(int newScore);
    public static event PollutionChangedHandler OnPollutionChanged;



    void Start()
    {
        currentForwardDirection = transform.forward;
        mainCamera = Camera.main;

        Camera.main.transform.position = CalculateCameraPosition();
        Camera.main.transform.LookAt(cameraTarget.position);

        nextSpawnTime = Time.time + CalculateSpawnInterval();
        currentMaxHealth = initialMaxHealth;
        currentHealth = initialMaxHealth;
        InitializeHealthUI();
        initialScore = score;
        // 初始化污染系统
        currentPollution = score; // 初始污染度 = 初始分数
        pollutionTimer = 0f;
        pollutionIncreaseRate = 1f; // 每分钟增加0.1污染度
    }

    void Update()
    {
        HandleMovement();
        HandleTrashSpawning();
        HandleTrashVisibility();
        UpdateHealthUI();
        
        // 应用击退效果
        if (knockbackVelocity.magnitude > 0.1f)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 5f * Time.deltaTime);
        }
        seecurrentMaxHp = currentMaxHealth;
        pollutionTimer += Time.deltaTime;
        if (pollutionTimer >= pollutionUpdateInterval)
        {
            pollutionTimer = 0f;

            // 指数增长模型：污染越高增长越快
            float dynamicRate = pollutionIncreaseRate * Mathf.Pow(1.25f, currentPollution);

            // 添加加速因子
            dynamicRate *= (1 + pollutionAcceleration * currentPollution);

            currentPollution = Mathf.Min(10f, currentPollution + dynamicRate);

            // 更新分数（四舍五入）
            int newScore = Mathf.RoundToInt(currentPollution);
            if (newScore != score)
            {
                UpdateScore(newScore);
            }

            // 调试信息
            Debug.Log($"污染增长: +{dynamicRate:F2} | 当前: {currentPollution:F2}/10");
        }
    }
    private int initialScore;
    void LateUpdate()
    {
        UpdateCamera();
    }

    void HandleMovement()
    {
        // 只有在没有击退效果时才能控制移动
        if (knockbackVelocity.magnitude < 0.1f)
        {
            transform.position += currentForwardDirection * moveSpeed * Time.deltaTime;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            float yaw = horizontal * rotationSpeed * Time.deltaTime;
            float pitch = vertical * pitchSpeed * Time.deltaTime;

            Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(-pitch, transform.right);

            currentForwardDirection = yawRotation * pitchRotation * currentForwardDirection;
            transform.rotation = Quaternion.LookRotation(currentForwardDirection);
        }
    }

    void UpdateCamera()
    {
        Vector3 targetPosition = CalculateCameraPosition();

        Camera.main.transform.position = Vector3.SmoothDamp(
            Camera.main.transform.position,
            targetPosition,
            ref cameraVelocity,
            rotationSmoothness
        );

        Vector3 lookDirection = cameraTarget.position - Camera.main.transform.position;
        targetCameraRotation = Quaternion.LookRotation(lookDirection);

        Camera.main.transform.rotation = Quaternion.Slerp(
            Camera.main.transform.rotation,
            targetCameraRotation,
            rotationSmoothness * Time.deltaTime * 10
        );
    }

    Vector3 CalculateCameraPosition()
    {
        Vector3 backOffset = -currentForwardDirection * cameraDistance;
        Vector3 heightOffset = Vector3.up * cameraHeight;
        return cameraTarget.position + backOffset + heightOffset;
    }

    // =============== 垃圾生成系统 ===============
    void HandleTrashSpawning()
    {
        if (Time.time >= nextSpawnTime && activeTrash.Count < maxTrashInView)
        {
            SpawnTrash();
            nextSpawnTime = Time.time + CalculateSpawnInterval();
        }
    }

    void SpawnTrash()
    {
        int unlockedLevel = GetUnlockedTrashLevel();
        int spawnCount = CalculateSpawnCount();
        
        Debug.Log($"生成垃圾: 数量={spawnCount} | 解锁等级={unlockedLevel}");
        
        for (int i = 0; i < spawnCount; i++)
        {
            float dangerLevel = CalculateDangerLevel();
            GameObject trashPrefab = SelectTrashPrefabByDangerLevel(dangerLevel, unlockedLevel);
            
            if (trashPrefab != null)
            {
                Vector3 spawnPosition = CalculateSphereSpawnPosition();
                GameObject trash = Instantiate(trashPrefab, spawnPosition, Quaternion.identity);
                activeTrash.Add(trash);
                
                // 调试信息
                TrashBehavior trashBehavior = trash.GetComponent<TrashBehavior>();
                if (trashBehavior != null)
                {
                    Debug.Log($"生成垃圾: 等级={(int)trashBehavior.trashLevel+1} " + 
                            $"(危险度:{dangerLevel:F2})");
                }
            }
        }
    }
    GameObject SelectTrashPrefabByDangerLevel(float dangerLevel)
    {
        // 根据危险度选择对应的预制体数组
        GameObject[] targetPrefabs = null;

        if (dangerLevel < 0.3f) targetPrefabs = level1TrashPrefabs;
        else if (dangerLevel < 0.5f) targetPrefabs = level2TrashPrefabs;
        else if (dangerLevel < 0.7f) targetPrefabs = level3TrashPrefabs;
        else if (dangerLevel < 0.9f) targetPrefabs = level4TrashPrefabs;
        else targetPrefabs = level5TrashPrefabs;

        // 从选定的数组中随机选择预制体
        if (targetPrefabs != null && targetPrefabs.Length > 0)
        {
            return targetPrefabs[Random.Range(0, targetPrefabs.Length)];
        }

        return null;
    }
    float CalculateDangerLevel()
    {
        float baseDanger = Mathf.Clamp01(score / 10f);
        
        // 基础危险度曲线
        float expDanger = Mathf.Pow(baseDanger, 0.7f);
        
        // 随机偏移（范围随污染度减小）
        float randomRange = Mathf.Lerp(0.4f, 0.1f, baseDanger);
        return Mathf.Clamp01(expDanger + Random.Range(-randomRange, randomRange));
    }
    Vector3 CalculateSphereSpawnPosition()
    {
        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;

        float randomAngleY = Random.Range(-spawnAngle, spawnAngle);
        float randomAngleX = Random.Range(-spawnAngle, spawnAngle);

        float radY = randomAngleY * Mathf.Deg2Rad;
        float radX = randomAngleX * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(
            Mathf.Sin(radX) * Mathf.Cos(radY),
            Mathf.Sin(radX) * Mathf.Sin(radY),
            Mathf.Cos(radX)
        );

        Quaternion cameraRotation = Quaternion.LookRotation(cameraForward);
        Vector3 cameraAlignedDirection = cameraRotation * direction;

        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);

        return cameraPos + cameraAlignedDirection * distance;
    }

    float CalculateSpawnInterval()
    {
        float intervalFactor = Mathf.Lerp(0.3f, 1f, score / 10f);
        return Random.Range(minSpawnInterval, maxSpawnInterval) * intervalFactor;
    }

    void HandleTrashVisibility()
    {
        List<GameObject> trashToRemove = new List<GameObject>();

        foreach (GameObject trash in activeTrash)
        {
            if (trash == null)
            {
                trashToRemove.Add(trash);
                continue;
            }

            // 检查垃圾是否在镜头视野内
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(trash.transform.position);
            bool inView = viewportPos.x > 0 && viewportPos.x < 1 &&
                        viewportPos.y > 0 && viewportPos.y < 1 &&
                        viewportPos.z > 0;

            // 设置垃圾可见性
            trash.SetActive(inView);

            // 如果垃圾在镜头外太远，移除它
            if (Vector3.Distance(trash.transform.position, transform.position) > spawnDistanceMax * 1.5f)
            {
                Destroy(trash);
                trashToRemove.Add(trash);
            }
        }

        // 清理已销毁的垃圾
        foreach (GameObject trash in trashToRemove)
        {
            activeTrash.Remove(trash);
        }
    }

    // =============== 分数管理 ===============
    // 修改UpdateScore方法
    public void UpdateScore(int newScore)
    {
        if (newScore == score) return;
        
        score = Mathf.Clamp(newScore, 0, 10);
        
        // 每2级污染度增加垃圾生成强度
        int pollutionTier = Mathf.FloorToInt(score / 2f);
        
        // 根据污染等级调整垃圾生成参数
        maxTrashInView = 10 + pollutionTier * 5;
        minSpawnInterval = 3.0f - pollutionTier * 0.5f;
        
        // 触发污染度变化事件
        if (OnPollutionChanged != null)
        {
            OnPollutionChanged(score);
        }
        
        Debug.Log($"污染等级更新: {score} | 解锁等级: {GetUnlockedTrashLevel()}");
    }

    // =============== 玩家生命管理 ===============
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        targetSliderValue = currentHealth;

        // 立即更新滑条值
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        UpdateHealthText();

        //Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{currentMaxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ReduceMaxHealth(int amount)
    {
        // 修复：更新实际最大生命值
        currentMaxHealth = Mathf.Max(10, currentMaxHealth - amount);

        // 确保当前生命值不超过新的最大生命值
        currentHealth = Mathf.Min(currentHealth, currentMaxHealth);
        targetSliderValue = currentHealth;

        // 立即更新UI
        if (healthSlider != null)
        {
            healthSlider.maxValue = currentMaxHealth;
            healthSlider.value = currentHealth;
        }

        UpdateHealthText();
        //Debug.Log($"Max health reduced by {amount}! New max: {currentMaxHealth}");
    }

    public void ApplyKnockback(Vector3 force)
    {
        knockbackVelocity = force;
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // 游戏结束逻辑
    }
    // =============== 生命值UI系统 ===============
    private void ForceUISync()
    {
        if (healthSlider != null)
        {
            // 如果滑条值与实际血量差异过大，直接同步
            if (Mathf.Abs(healthSlider.value - currentHealth) > 5f)
            {
                healthSlider.value = currentHealth;
            }

            // 确保滑块最大值与实际最大血量一致
            if (Mathf.Abs(healthSlider.maxValue - currentMaxHealth) > 0.1f)
            {
                healthSlider.maxValue = currentMaxHealth;
            }
        }

        // 确保文本与实际血量一致
        UpdateHealthText();
    }
    void InitializeHealthUI()
    {
        if (healthSlider != null)
        {
            // 修复：使用 currentMaxHealth 而不是 maxHealth
            healthSlider.minValue = 0;
            healthSlider.maxValue = currentMaxHealth;
            healthSlider.value = currentHealth;
            targetSliderValue = currentHealth;

            // 获取填充图像组件
            sliderFillImage = healthSlider.fillRect.GetComponent<Image>();
            if (sliderFillImage != null)
            {
                originalFillColor = sliderFillImage.color;
            }
        }

        void UpdateHealthText()
        {
            if (healthText != null)
            {
                // 修复：使用 currentMaxHealth 而不是 maxHealth
                healthText.text = $"{currentHealth}/{currentMaxHealth}";

                healthText.color = isLowHealth ? lowHealthColor : normalTextColor;
            }
        }

        // 更新文本显示
        UpdateHealthText();

        // 初始化UI同步计时器
        lastHealthUpdateTime = Time.time;
    }

    void UpdateHealthUI()
    {
        // 平滑过渡滑条值（仅在血量恢复时）
        if (healthSlider != null && healthSlider.value < targetSliderValue)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetSliderValue, smoothSpeed * Time.deltaTime);
        }

        // 检查低生命值状态
        bool nowLowHealth = currentHealth <= lowHealthThreshold;

        // 状态变化时启动/停止闪烁
        if (nowLowHealth && !isLowHealth)
        {
            // 进入低生命值状态
            isLowHealth = true;
            if (blinkCoroutine == null)
            {
                blinkCoroutine = StartCoroutine(BlinkHealthUI());
            }
        }
        else if (!nowLowHealth && isLowHealth)
        {
            // 退出低生命值状态
            isLowHealth = false;
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            ResetHealthUIColors();
        }

        // 确保文本始终可见
        UpdateHealthText();
    }

    void UpdateHealthText()
    {
        if (healthText != null)
        {
            // 直接使用当前血量值，而不是滑块值
            healthText.text = $"{currentHealth}/{currentMaxHealth}";

            // 根据生命值状态设置文本颜色
            healthText.color = isLowHealth ? lowHealthColor : normalTextColor;
        }
    }

    IEnumerator BlinkHealthUI()
    {
        bool blinkState = true;

        while (isLowHealth)
        {
            // 切换闪烁状态
            blinkState = !blinkState;

            // 更新UI颜色 - 只闪烁滑块填充部分
            if (sliderFillImage != null)
            {
                sliderFillImage.color = blinkState ? lowHealthColor : originalFillColor;
            }

            // 文本保持红色可见（不参与闪烁）
            if (healthText != null)
            {
                healthText.color = lowHealthColor;
            }

            // 等待下次闪烁
            yield return new WaitForSeconds(blinkInterval);
        }

        // 退出时重置颜色
        ResetHealthUIColors();
    }

    void ResetHealthUIColors()
    {
        if (sliderFillImage != null)
            sliderFillImage.color = originalFillColor;

        if (healthText != null)
            healthText.color = isLowHealth ? lowHealthColor : normalTextColor;
    }
    private int GetUnlockedTrashLevel()
    {
        // 每2级污染度解锁一个危险等级
        // 污染度0-1: 只解锁1级
        // 污染度2-3: 解锁1-2级
        // 污染度4-5: 解锁1-3级
        // 污染度6-7: 解锁1-4级
        // 污染度8-10: 解锁1-5级
        return Mathf.Clamp(1 + Mathf.FloorToInt(score / 2), 1, 5);
    }
    private int CalculateSpawnCount()
    {
        // 污染度0-1: 生成1-2个垃圾
        // 污染度2-3: 生成2-3个垃圾
        // 污染度4-5: 生成3-4个垃圾
        // 污染度6-7: 生成4-5个垃圾
        // 污染度8-10: 生成5-6个垃圾
        int baseCount = 1 + Mathf.FloorToInt(score / 2f);
        
        // 添加随机性：±1
        return Random.Range(baseCount, baseCount + 2);
    }
    GameObject SelectTrashPrefabByDangerLevel(float dangerLevel, int maxAllowedLevel)
    {
        // 根据最大允许等级调整危险度范围
        float adjustedDanger = dangerLevel * (maxAllowedLevel / 5f);

        // 根据调整后的危险度选择等级
        if (adjustedDanger < 0.3f) return RandomTrash(level1TrashPrefabs);
        if (adjustedDanger < 0.5f) return RandomTrash(level2TrashPrefabs);
        if (adjustedDanger < 0.7f) return RandomTrash(level3TrashPrefabs);
        if (adjustedDanger < 0.9f) return RandomTrash(level4TrashPrefabs);
        return RandomTrash(level5TrashPrefabs);
    }
    GameObject RandomTrash(GameObject[] array)
    {
        return array.Length > 0 ? array[Random.Range(0, array.Length)] : null;
    }

}