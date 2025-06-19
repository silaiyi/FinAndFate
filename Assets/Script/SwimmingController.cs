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
    public int score = 10;

    [Header("Player Health Settings")]
    public int initialMaxHealth = 100;
    public int seecurrentMaxHp;
    public int currentHealth;
    private int currentMaxHealth;
    private Vector3 knockbackVelocity;
    private Vector3 currentForwardDirection;
    private Quaternion targetCameraRotation;
    private Vector3 cameraVelocity;
    private List<GameObject> activeTrash = new List<GameObject>();
    private float nextSpawnTime;
    private Camera mainCamera;

    [Header("Health UI Settings")]
    public Slider healthSlider;
    public Text healthText;
    public Color normalTextColor = Color.white;
    public Color lowHealthColor = Color.red;
    public float smoothSpeed = 25f;
    public float blinkInterval = 0.5f;
    public int lowHealthThreshold = 40;

    [Header("Pollution Progression")]
    public float pollutionIncreaseRate = 1.0f;
    public float pollutionAcceleration = 0.2f;
    public float pollutionUpdateInterval = 10f;
    
    private bool isLowHealth = false;
    private Coroutine blinkCoroutine;
    private Image sliderFillImage;
    private Color originalFillColor;
    private float pollutionTimer;
    private float currentPollution;
    private float targetSliderValue;

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
        currentPollution = score;
        pollutionTimer = 0f;
    }

    void Update()
    {
        HandleMovement();
        HandleTrashSpawning();
        HandleTrashVisibility();
        UpdateHealthUI();
        
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
            float dynamicRate = pollutionIncreaseRate * Mathf.Pow(1.25f, currentPollution);
            dynamicRate *= (1 + pollutionAcceleration * currentPollution);
            currentPollution = Mathf.Min(10f, currentPollution + dynamicRate);
            int newScore = Mathf.RoundToInt(currentPollution);
            
            if (newScore != score)
            {
                UpdateScore(newScore);
            }
        }
    }

    void LateUpdate() => UpdateCamera();

    void HandleMovement()
    {
        if (knockbackVelocity.magnitude < 0.1f)
        {
            transform.position += currentForwardDirection * moveSpeed * Time.deltaTime;

            float yaw = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
            float pitch = Input.GetAxis("Vertical") * pitchSpeed * Time.deltaTime;

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

    Vector3 CalculateCameraPosition() => 
        cameraTarget.position + (-currentForwardDirection * cameraDistance) + (Vector3.up * cameraHeight);

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

    float CalculateDangerLevel()
    {
        float baseDanger = Mathf.Clamp01(score / 10f);
        float expDanger = Mathf.Pow(baseDanger, 0.7f);
        float randomRange = Mathf.Lerp(0.4f, 0.1f, baseDanger);
        return Mathf.Clamp01(expDanger + Random.Range(-randomRange, randomRange));
    }

    Vector3 CalculateSphereSpawnPosition()
    {
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

    public void UpdateScore(int newScore)
    {
        if (newScore == score) return;
        score = Mathf.Clamp(newScore, 0, 10);
        int pollutionTier = Mathf.FloorToInt(score / 2f);
        
        maxTrashInView = 10 + pollutionTier * 5;
        minSpawnInterval = 3.0f - pollutionTier * 0.5f;
        
        OnPollutionChanged?.Invoke(score);
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        targetSliderValue = currentHealth;

        if (healthSlider != null) healthSlider.value = currentHealth;
        UpdateHealthText();

        if (currentHealth <= 0) Die();
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
    private void Die() => Debug.Log("Player died!");

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

    int GetUnlockedTrashLevel() => Mathf.Clamp(1 + Mathf.FloorToInt(score / 2), 1, 5);
    int CalculateSpawnCount() => Random.Range(1 + Mathf.FloorToInt(score / 2f), 3 + Mathf.FloorToInt(score / 2f));

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
}