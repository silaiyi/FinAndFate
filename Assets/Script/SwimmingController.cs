using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SwimmingController : MonoBehaviour
{
    #region Singleton
    public static SwimmingController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 确保跨场景时对象不被销毁
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Movement Settings
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 120.0f;
    public float pitchSpeed = 60.0f;
    #endregion

    #region Camera Settings
    [Header("Camera Settings")]
    public Transform cameraTarget;
    public float cameraDistance = 5.0f;
    public float cameraHeight = 2.0f;
    public float rotationSmoothness = 0.1f;
    #endregion

    #region Trash Generation
    [Header("Trash Generation Settings")]
    public GameObject[] level1TrashPrefabs;
    public GameObject[] level2TrashPrefabs;
    public GameObject[] level3TrashPrefabs;
    public GameObject[] level4TrashPrefabs;
    public GameObject[] level5TrashPrefabs;
    public float minSpawnInterval = 3.0f;
    public float maxSpawnInterval = 10.0f;
    public int maxTrashInView = 20;
    public float spawnDistanceMin = 10f;
    public float spawnDistanceMax = 30f;
    public float spawnAngle = 60f;
    #endregion

    #region Pollution Scores
    [Header("Pollution Scores")]
    public int carbonScore = 0;
    public int trashScore = 0;
    public int fishingScore = 0;
    public int sewageScore = 0;
    #endregion

    #region Player Health
    [Header("Player Health Settings")]
    public int initialMaxHealth = 100;
    public int seecurrentMaxHp;
    public int currentHealth;
    private int currentMaxHealth;
    #endregion

    #region UI Settings
    [Header("Health UI Settings")]
    public Slider healthSlider;
    public Text healthText;
    public Color normalTextColor = Color.white;
    public Color lowHealthColor = Color.red;
    public float smoothSpeed = 25f;
    public float blinkInterval = 0.5f;
    public int lowHealthThreshold = 40;
    #endregion

    #region Pollution Progression
    [Header("Pollution Progression")]
    public float pollutionIncreaseRate = 1.0f;
    public float pollutionAcceleration = 0.2f;
    public float pollutionUpdateInterval = 10f;
    #endregion

    #region Stealth Settings
    [Header("Stealth Settings")]
    public bool isHiding = false;
    public ObstacleType currentObstacleType = ObstacleType.None;
    public GameObject currentHideObstacle;
    #endregion

    #region Coral Damage
    [Header("Coral Damage Settings")]
    public float coralDamageInterval = 1.0f;
    public int baseCoralDamage = 10;
    private float nextCoralDamageTime;
    #endregion

    #region Water Surface
    [Header("Water Surface Settings")]
    public float waterSurfaceY = 0f;
    public float minDepth = -1f;
    public float surfacePushForce = 5f;
    #endregion

    #region Food Settings
    [Header("Food Settings")]
    public GameObject[] foodVarieties;
    public int maxFoodInView = 5;
    public float minFoodSpawnInterval = 5.0f;
    public float maxFoodSpawnInterval = 15.0f;
    public float foodSpawnDistanceMin = 10f;
    public float foodSpawnDistanceMax = 30f;
    public float foodSpawnAngle = 60f;
    #endregion

    #region Collision Settings
    [Header("Collision Settings")]
    public float collisionForce = 5f;
    public LayerMask terrainLayer;
    #endregion

    #region Private Variables
    // Movement & Physics
    private Vector3 knockbackVelocity;
    private Vector3 currentForwardDirection;
    private Quaternion targetCameraRotation;
    private Vector3 cameraVelocity;
    private Rigidbody rb;
    private Vector3 lastSafePosition;

    // Trash & Food
    private List<GameObject> activeTrash = new List<GameObject>();
    private List<GameObject> activeFood = new List<GameObject>();
    private float nextSpawnTime;
    private float nextFoodSpawnTime;

    // Camera
    private Camera mainCamera;

    // Health UI
    private bool isLowHealth = false;
    private Coroutine blinkCoroutine;
    private Image sliderFillImage;
    private Color originalFillColor;
    private float pollutionTimer;
    private float currentPollution;
    private float targetSliderValue;

    // Pollution
    private PollutionScores currentPollutionScores;
    #endregion

    #region Structures & Enums
    public struct PollutionScores
    {
        public int carbon;
        public int trash;
        public int fishing;
        public int sewage;
    }

    public delegate void PollutionChangedHandler(PollutionScores newScores);
    public static event PollutionChangedHandler OnPollutionChanged;

    public enum ObstacleType { None, Coral, Rock }
    #endregion
    public float fishingScoreIncreaseMultiplier = 1.5f;
    private float carbonFraction;
    private float trashFraction;
    private float fishingFraction;
    private float sewageFraction;

    /******************************************************************
     * INITIALIZATION & CORE GAMEPLAY LOOP
     ******************************************************************/
    void Start()
    {
        InitializeMovement();
        InitializeCamera();
        InitializePollutionScores();
        InitializeHealth();
        InitializeFoodSystem();
        InitializePhysics();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleCollisionRecovery();
    }

    void Update()
    {
        HandleTrashSpawning();
        HandleTrashVisibility();
        UpdateHealthUI();
        HandleFoodSpawning();
        HandleFoodVisibility();
        HandleKnockback();
        UpdatePollutionOverTime();
        HandleCoralDamage();
        LimitPlayerDepth();
    }

    void LateUpdate() => UpdateCamera();

    /******************************************************************
     * INITIALIZATION METHODS
     ******************************************************************/
    private void InitializeMovement()
    {
        currentForwardDirection = transform.forward;
    }

    private void InitializeCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = CalculateCameraPosition();
            mainCamera.transform.LookAt(cameraTarget.position);
        }
    }

    private void InitializePollutionScores()
    {
        // 关键修改：从PlayerPrefs加载问卷分数
        if (PlayerPrefs.HasKey("CarbonScore"))
        {
            carbonScore = PlayerPrefs.GetInt("CarbonScore");
            trashScore = PlayerPrefs.GetInt("TrashScore");
            fishingScore = PlayerPrefs.GetInt("FishingScore");
            sewageScore = PlayerPrefs.GetInt("SewageScore");

            Debug.Log($"Loaded pollution scores from PlayerPrefs: " +
                     $"C={carbonScore}, T={trashScore}, " +
                     $"F={fishingScore}, S={sewageScore}");
        }

        currentPollutionScores = new PollutionScores
        {
            carbon = carbonScore,
            trash = trashScore,
            fishing = fishingScore,
            sewage = sewageScore
        };

        // 触发初始污染事件
        OnPollutionChanged?.Invoke(currentPollutionScores);
        currentPollution = (carbonScore + trashScore + fishingScore + sewageScore) / 4f;
        pollutionTimer = 0f;
    }

    private void InitializeHealth()
    {
        currentMaxHealth = initialMaxHealth;
        currentHealth = initialMaxHealth;
        InitializeHealthUI();
        nextCoralDamageTime = Time.time + coralDamageInterval;
        seecurrentMaxHp = currentMaxHealth;
    }

    private void InitializeFoodSystem()
    {
        nextFoodSpawnTime = Time.time + CalculateFoodSpawnInterval();
    }

    private void InitializePhysics()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        lastSafePosition = transform.position;
    }

    /******************************************************************
     * MOVEMENT & CAMERA CONTROLS
     ******************************************************************/
    void HandleMovement()
    {
        if (knockbackVelocity.magnitude < 0.1f)
        {
            Vector3 moveDirection = currentForwardDirection * moveSpeed * Time.fixedDeltaTime;
            if (rb != null)
            {
                rb.MovePosition(rb.position + moveDirection);
            }
            else
            {
                transform.position += moveDirection;
            }

            float yaw = Input.GetAxis("Horizontal") * rotationSpeed * Time.fixedDeltaTime;
            float pitch = Input.GetAxis("Vertical") * pitchSpeed * Time.fixedDeltaTime;

            Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(-pitch, transform.right);

            currentForwardDirection = yawRotation * pitchRotation * currentForwardDirection;

            if (rb != null)
            {
                rb.MoveRotation(Quaternion.LookRotation(currentForwardDirection));
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(currentForwardDirection);
            }
        }
        else
        {
            if (rb != null)
            {
                rb.MovePosition(rb.position + knockbackVelocity * Time.fixedDeltaTime);
            }
            else
            {
                transform.position += knockbackVelocity * Time.fixedDeltaTime;
            }
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 5f * Time.fixedDeltaTime);
        }
    }

    void UpdateCamera()
    {
        if (mainCamera == null || cameraTarget == null) return;

        Vector3 targetPosition = CalculateCameraPosition();
        mainCamera.transform.position = Vector3.SmoothDamp(
            mainCamera.transform.position,
            targetPosition,
            ref cameraVelocity,
            rotationSmoothness
        );

        Vector3 lookDirection = cameraTarget.position - mainCamera.transform.position;
        targetCameraRotation = Quaternion.LookRotation(lookDirection);
        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetCameraRotation,
            rotationSmoothness * Time.deltaTime * 10
        );
    }

    Vector3 CalculateCameraPosition() =>
        cameraTarget.position + (-currentForwardDirection * cameraDistance) + (Vector3.up * cameraHeight);

    /******************************************************************
     * TRASH SYSTEM
     ******************************************************************/
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

        for (int i = 0; i < spawnCount; i++)
        {
            float dangerLevel = CalculateDangerLevel();
            GameObject trashPrefab = SelectTrashPrefabByDangerLevel(dangerLevel, unlockedLevel);

            if (trashPrefab != null)
            {
                Vector3 spawnPosition = CalculateSphereSpawnPosition();
                GameObject trash = Instantiate(trashPrefab, spawnPosition, Quaternion.identity);
                activeTrash.Add(trash);
            }
        }
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

            Vector3 viewportPos = mainCamera.WorldToViewportPoint(trash.transform.position);
            bool inView = viewportPos.x > 0 && viewportPos.x < 1 &&
                        viewportPos.y > 0 && viewportPos.y < 1 &&
                        viewportPos.z > 0;

            trash.SetActive(inView);

            if (Vector3.Distance(trash.transform.position, transform.position) > spawnDistanceMax * 1.5f)
            {
                Destroy(trash);
                trashToRemove.Add(trash);
            }
        }

        foreach (GameObject trash in trashToRemove)
        {
            activeTrash.Remove(trash);
        }
    }

    float CalculateDangerLevel()
    {
        float baseDanger = Mathf.Clamp01(trashScore / 10f);
        float expDanger = Mathf.Pow(baseDanger, 0.7f);
        float randomRange = Mathf.Lerp(0.4f, 0.1f, baseDanger);
        return Mathf.Clamp01(expDanger + Random.Range(-randomRange, randomRange));
    }

    Vector3 CalculateSphereSpawnPosition()
    {
        if (mainCamera == null) return transform.position;

        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;

        float randomAngleY = Random.Range(-spawnAngle, spawnAngle) * Mathf.Deg2Rad;
        float randomAngleX = Random.Range(-spawnAngle, spawnAngle) * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(
            Mathf.Sin(randomAngleX) * Mathf.Cos(randomAngleY),
            Mathf.Sin(randomAngleX) * Mathf.Sin(randomAngleY),
            Mathf.Cos(randomAngleX)
        );

        Quaternion cameraRotation = Quaternion.LookRotation(cameraForward);
        Vector3 cameraAlignedDirection = cameraRotation * direction;
        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);

        return cameraPos + cameraAlignedDirection * distance;
    }

    float CalculateSpawnInterval()
    {
        float intervalFactor = Mathf.Lerp(0.3f, 1f, trashScore / 10f);
        return Random.Range(minSpawnInterval, maxSpawnInterval) * intervalFactor;
    }

    int GetUnlockedTrashLevel() => Mathf.Clamp(1 + Mathf.FloorToInt(trashScore / 2), 1, 5);
    int CalculateSpawnCount() => Random.Range(1 + Mathf.FloorToInt(trashScore / 2f), 3 + Mathf.FloorToInt(trashScore / 2f));

    GameObject SelectTrashPrefabByDangerLevel(float dangerLevel, int maxAllowedLevel)
    {
        float adjustedDanger = dangerLevel * (maxAllowedLevel / 5f);
        GameObject[] targetPrefabs = null;

        if (adjustedDanger < 0.3f) targetPrefabs = level1TrashPrefabs;
        else if (adjustedDanger < 0.5f) targetPrefabs = level2TrashPrefabs;
        else if (adjustedDanger < 0.7f) targetPrefabs = level3TrashPrefabs;
        else if (adjustedDanger < 0.9f) targetPrefabs = level4TrashPrefabs;
        else targetPrefabs = level5TrashPrefabs;

        return (targetPrefabs != null && targetPrefabs.Length > 0) ?
            targetPrefabs[Random.Range(0, targetPrefabs.Length)] :
            null;
    }

    /******************************************************************
     * FOOD SYSTEM
     ******************************************************************/
    void HandleFoodSpawning()
    {
        if (Time.time >= nextFoodSpawnTime && activeFood.Count < maxFoodInView)
        {
            float totalScore = (carbonScore + trashScore + fishingScore + sewageScore) / 4f;
            float spawnChance = Mathf.Lerp(0.7f, 0.01f, totalScore / 10f);

            if (Random.value <= spawnChance)
            {
                SpawnFood();
            }

            nextFoodSpawnTime = Time.time + CalculateFoodSpawnInterval();
        }
    }

    void SpawnFood()
    {
        Vector3 spawnPosition = CalculateFoodSpawnPosition();
        GameObject foodPrefab = foodVarieties[Random.Range(0, foodVarieties.Length)];
        GameObject food = Instantiate(foodPrefab, spawnPosition, Quaternion.identity);
        activeFood.Add(food);
    }

    void HandleFoodVisibility()
    {
        List<GameObject> foodToRemove = new List<GameObject>();

        foreach (GameObject food in activeFood)
        {
            if (food == null)
            {
                foodToRemove.Add(food);
                continue;
            }

            Vector3 viewportPos = mainCamera.WorldToViewportPoint(food.transform.position);
            bool inView = viewportPos.x > 0 && viewportPos.x < 1 &&
                        viewportPos.y > 0 && viewportPos.y < 1 &&
                        viewportPos.z > 0;

            food.SetActive(inView);

            if (Vector3.Distance(food.transform.position, transform.position) > 200f)
            {
                Destroy(food);
                foodToRemove.Add(food);
            }
        }

        foreach (GameObject food in foodToRemove)
        {
            activeFood.Remove(food);
        }
    }

    Vector3 CalculateFoodSpawnPosition()
    {
        if (mainCamera == null) return transform.position;

        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;

        float randomAngleY = Random.Range(-foodSpawnAngle, foodSpawnAngle) * Mathf.Deg2Rad;
        float randomAngleX = Random.Range(-foodSpawnAngle, foodSpawnAngle) * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(
            Mathf.Sin(randomAngleX) * Mathf.Cos(randomAngleY),
            Mathf.Sin(randomAngleX) * Mathf.Sin(randomAngleY),
            Mathf.Cos(randomAngleX)
        );

        Quaternion cameraRotation = Quaternion.LookRotation(cameraForward);
        Vector3 cameraAlignedDirection = cameraRotation * direction;
        float distance = Random.Range(foodSpawnDistanceMin, foodSpawnDistanceMax);

        return cameraPos + cameraAlignedDirection * distance;
    }

    float CalculateFoodSpawnInterval() =>
        Random.Range(minFoodSpawnInterval, maxFoodSpawnInterval);

    /******************************************************************
     * HEALTH SYSTEM
     ******************************************************************/
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        targetSliderValue = currentHealth;

        if (healthSlider != null) healthSlider.value = currentHealth;
        UpdateHealthText();

        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentMaxHealth, currentHealth + amount);
        targetSliderValue = currentHealth;

        if (healthSlider != null) healthSlider.value = currentHealth;
        UpdateHealthText();
    }

    public void ReduceMaxHealth(int amount)
    {
        currentMaxHealth = Mathf.Max(10, currentMaxHealth - amount);
        currentHealth = Mathf.Min(currentHealth, currentMaxHealth);
        targetSliderValue = currentHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = currentMaxHealth;
            healthSlider.value = currentHealth;
        }

        UpdateHealthText();
    }

    public void ApplyKnockback(Vector3 force) => knockbackVelocity = force;

    private void Die()
    {
        Debug.Log("Player died!");
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LevelComplete(false);
        }
    }

    void InitializeHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = currentMaxHealth;
            healthSlider.value = currentHealth;
            targetSliderValue = currentHealth;
            sliderFillImage = healthSlider.fillRect.GetComponent<Image>();
            if (sliderFillImage != null) originalFillColor = sliderFillImage.color;
        }
        UpdateHealthText();
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Lerp(
                healthSlider.value,
                targetSliderValue,
                smoothSpeed * Time.deltaTime
            );
        }

        bool nowLowHealth = currentHealth <= lowHealthThreshold;
        if (nowLowHealth && !isLowHealth)
        {
            isLowHealth = true;
            if (blinkCoroutine == null) blinkCoroutine = StartCoroutine(BlinkHealthUI());
        }
        else if (!nowLowHealth && isLowHealth)
        {
            isLowHealth = false;
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
            ResetHealthUIColors();
        }
        UpdateHealthText();
    }

    void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{currentMaxHealth}";
            healthText.color = isLowHealth ? lowHealthColor : normalTextColor;
        }
    }

    IEnumerator BlinkHealthUI()
    {
        bool blinkState = true;
        while (isLowHealth)
        {
            blinkState = !blinkState;
            if (sliderFillImage != null)
                sliderFillImage.color = blinkState ? lowHealthColor : originalFillColor;
            yield return new WaitForSeconds(blinkInterval);
        }
        ResetHealthUIColors();
    }

    void ResetHealthUIColors()
    {
        if (sliderFillImage != null) sliderFillImage.color = originalFillColor;
        if (healthText != null) healthText.color = isLowHealth ? lowHealthColor : normalTextColor;
    }

    /******************************************************************
     * POLLUTION SYSTEM
     ******************************************************************/
    void UpdatePollutionOverTime()
    {
        pollutionTimer += Time.deltaTime;
        if (pollutionTimer >= pollutionUpdateInterval)
        {
            pollutionTimer = 0f;
            float totalScore = (carbonScore + trashScore + fishingScore + sewageScore) / 4f;
            float dynamicRate = pollutionIncreaseRate * Mathf.Pow(1.25f, totalScore);
            dynamicRate *= (1 + pollutionAcceleration * totalScore);

            // 計算浮點數增量
            float carbonIncrease = dynamicRate * 0.25f;
            float trashIncrease = dynamicRate * 0.25f;
            float fishingIncrease = dynamicRate * 0.25f;
            float sewageIncrease = dynamicRate * 0.25f;

            // 累積小數部分
            carbonFraction += carbonIncrease;
            trashFraction += trashIncrease;
            fishingFraction += fishingIncrease;
            sewageFraction += sewageIncrease;

            // 處理整數增量
            int carbonInt = (int)carbonFraction;
            int trashInt = (int)trashFraction;
            int fishingInt = (int)fishingFraction;
            int sewageInt = (int)sewageFraction;

            // 更新分數（限制最大值30）
            carbonScore = Mathf.Min(carbonScore + carbonInt, 10);
            trashScore = Mathf.Min(trashScore + trashInt, 10);
            fishingScore = Mathf.Min(fishingScore + fishingInt, 10);
            sewageScore = Mathf.Min(sewageScore + sewageInt, 10);

            // 保留小數部分
            carbonFraction -= carbonInt;
            trashFraction -= trashInt;
            fishingFraction -= fishingInt;
            sewageFraction -= sewageInt;

            // 觸發事件
            PollutionScores newScores = new PollutionScores
            {
                carbon = carbonScore,
                trash = trashScore,
                fishing = fishingScore,
                sewage = sewageScore
            };
            OnPollutionChanged?.Invoke(newScores);

            Debug.Log($"Pollution increased: C+{carbonInt} T+{trashInt} F+{fishingInt} S+{sewageInt}");
        }
    }

    public void UpdatePollutionScores(PollutionScores newScores)
    {
        carbonScore = newScores.carbon;
        trashScore = newScores.trash;
        fishingScore = newScores.fishing;
        sewageScore = newScores.sewage;
        carbonFraction = 0f;
        trashFraction = 0f;
        fishingFraction = 0f;
        sewageFraction = 0f;

        currentPollutionScores = newScores;

        // 更新污染效果
        int pollutionTier = Mathf.FloorToInt((carbonScore + trashScore + fishingScore + sewageScore) / 8f);
        maxTrashInView = 10 + pollutionTier * 5;
        minSpawnInterval = 3.0f - pollutionTier * 0.5f;

        Debug.Log($"Updated pollution scores: " +
                $"Carbon={carbonScore}, Trash={trashScore}, " +
                $"Fishing={fishingScore}, Sewage={sewageScore}");

        // 触发污染变化事件
        OnPollutionChanged?.Invoke(newScores);
    }

    public PollutionScores GetCurrentPollutionScores() => currentPollutionScores;

    /******************************************************************
     * CORAL DAMAGE SYSTEM
     ******************************************************************/
    void HandleCoralDamage()
    {
        if (carbonScore >= 4 && Time.time >= nextCoralDamageTime)
        {
            ApplyCoralDamage();
        }
    }

    void ApplyCoralDamage()
    {
        // 计算伤害值：10 * 2^(污染等级-4)
        int damage = baseCoralDamage * (int)Mathf.Pow(2, carbonScore - 4);

        // 查找所有珊瑚
        GameObject[] corals = GameObject.FindGameObjectsWithTag("CoralObstacle");
        Debug.Log($"Applying {damage} damage to {corals.Length} corals at carbon level {carbonScore}");

        foreach (GameObject coral in corals)
        {
            Obstacle obstacle = coral.GetComponent<Obstacle>();
            if (obstacle != null && obstacle.obstacleType == Obstacle.ObstacleType.Coral)
            {
                obstacle.TakeDamage(damage);
            }
        }

        nextCoralDamageTime = Time.time + coralDamageInterval;
    }

    /******************************************************************
     * WATER SURFACE MANAGEMENT
     ******************************************************************/
    void LimitPlayerDepth()
    {
        if (transform.position.y > minDepth)
        {
            transform.position = new Vector3(
                transform.position.x,
                minDepth,
                transform.position.z
            );
            ApplySurfacePushForce();
        }
    }

    void ApplySurfacePushForce()
    {
        if (Mathf.Abs(currentForwardDirection.y) > 0.1f)
        {
            Vector3 pushForce = Vector3.down * surfacePushForce * Time.deltaTime;
            currentForwardDirection += pushForce;
            currentForwardDirection.Normalize();
        }
    }

    /******************************************************************
     * COLLISION & PHYSICS
     ******************************************************************/
    void HandleCollisionRecovery()
    {
        if (IsPlayerStuck())
        {
            transform.position = lastSafePosition;
            if (rb != null) rb.velocity = Vector3.zero;
        }
        else
        {
            lastSafePosition = transform.position;
        }
    }

    bool IsPlayerStuck() =>
        Physics.CheckSphere(transform.position, 0.5f, terrainLayer);

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Terrain"))
        {
            Vector3 pushDirection = collision.contacts[0].normal;
            if (rb != null)
            {
                rb.AddForce(pushDirection * collisionForce, ForceMode.VelocityChange);
            }
            knockbackVelocity = Vector3.zero;
        }
    }

    void HandleKnockback()
    {
        if (knockbackVelocity.magnitude > 0.1f)
        {
            if (rb != null)
            {
                rb.MovePosition(rb.position + knockbackVelocity * Time.deltaTime);
            }
            else
            {
                transform.position += knockbackVelocity * Time.deltaTime;
            }
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 5f * Time.deltaTime);
        }
    }

    /******************************************************************
     * STEALTH SYSTEM
     ******************************************************************/
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CoralObstacle"))
        {
            isHiding = true;
            currentObstacleType = ObstacleType.Coral;
            currentHideObstacle = other.gameObject;
        }
        else if (other.CompareTag("RockObstacle"))
        {
            isHiding = true;
            currentObstacleType = ObstacleType.Rock;
            currentHideObstacle = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("CoralObstacle") || other.CompareTag("RockObstacle"))
        {
            isHiding = false;
            currentObstacleType = ObstacleType.None;
            currentHideObstacle = null;
        }
    }

    /******************************************************************
     * SPECIAL ACTIONS
     ******************************************************************/
    public void InstantDeath()
    {
        currentHealth = 0;
        Die();
        Debug.Log("InstantDeath called");
        if (TryGetComponent(out Renderer renderer))
        {
            renderer.material.color = Color.red;
        }
    }
    // SwimmingController.cs
    public void ResetPlayerState()
    {
        currentHealth = currentMaxHealth;
        targetSliderValue = currentHealth;
        isHiding = false;
        currentObstacleType = ObstacleType.None;
        currentHideObstacle = null;
        knockbackVelocity = Vector3.zero;
        
        // 重置UI
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
            healthSlider.maxValue = currentMaxHealth;
        }
        UpdateHealthText();
    }
}