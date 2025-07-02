/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DirectionIndicator : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform indicatorArrow;
    public Image arrowImage;
    public Text distanceText;

    [Header("Settings")]
    public float edgePadding = 50f;
    public float minDistance = 5f;
    public float maxDistance = 100f;
    public float minScale = 0.5f;
    public float maxScale = 1.0f;
    public float minAlpha = 0.5f;
    public float maxAlpha = 1.0f;
    public float pulseSpeed = 1.5f;
    public Vector3 screenOffset = new Vector3(0, 0, 0);

    private Transform target;
    private Camera mainCamera;
    private float pulseTime;
    [Header("Player Reference")]
    public Transform playerTransform;
    
    private bool _isActive = true;
    private CheckpointManager checkpointManager; // 添加檢查點管理器引用

    void Start()
    {
        mainCamera = Camera.main;
        pulseTime = 0f;
        
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // 初始化檢查點管理器（第三關專用）
        if (SceneManager.GetActiveScene().name == "Level3")
        {
            checkpointManager = FindObjectOfType<CheckpointManager>();
        }
        
        FindCompanionTarget();
        LevelManager.OnLevelComplete += HandleLevelComplete;
    }
    
    void OnDestroy()
    {
        LevelManager.OnLevelComplete -= HandleLevelComplete;
    }
    
    private void HandleLevelComplete(bool success)
    {
        _isActive = false;
        SetIndicatorVisible(false);
    }
    
    private void SetIndicatorVisible(bool visible)
    {
        if (indicatorArrow != null) indicatorArrow.gameObject.SetActive(visible);
        if (distanceText != null) distanceText.gameObject.SetActive(visible);
    }
    
    void FindCompanionTarget()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.companionInstance != null)
        {
            target = LevelManager.Instance.companionInstance.transform;
            return;
        }

        GameObject companion = GameObject.FindGameObjectWithTag("Companion");
        if (companion != null) 
        {
            target = companion.transform;
            return;
        }

        Debug.LogWarning("Companion target not found!");
    }

    void Update()
    {
        if (!_isActive) return;
        
        // === 修改部分：處理第三關檢查點目標 ===
        if (SceneManager.GetActiveScene().name == "Level3" && checkpointManager != null)
        {
            Transform checkpointTarget = checkpointManager.GetCurrentCheckpoint();
            if (checkpointTarget != null)
            {
                // 使用檢查點作為目標
                UpdateIndicator(checkpointTarget);
                return;
            }
        }
        
        // 原有同伴目標處理
        if (target == null) 
        {
            if (LevelManager.Instance != null && LevelManager.Instance.companionInstance != null)
            {
                target = LevelManager.Instance.companionInstance.transform;
            }
            return;
        }
        
        if (playerTransform == null) return;

        UpdateIndicator(target); // 傳入當前目標
        UpdatePulseEffect();
    }

    // 修改方法：接受目標參數
    void UpdateIndicator(Transform currentTarget)
    {
        if (currentTarget == null) return;
        
        float currentDistance = Vector3.Distance(playerTransform.position, currentTarget.position);
        
        if (distanceText != null)
        {
            distanceText.text = Mathf.RoundToInt(currentDistance) + "m";
        }
        
        Vector3 screenPos = mainCamera.WorldToViewportPoint(currentTarget.position);
        bool isTargetVisible = screenPos.z > 0 && screenPos.x > 0 && screenPos.x < 1 && screenPos.y > 0 && screenPos.y < 1;
        
        Vector3 screenPosAdjusted;
        
        if (isTargetVisible)
        {
            screenPosAdjusted = new Vector3(
                screenPos.x,
                screenPos.y + screenOffset.y,
                0
            );
            
            indicatorArrow.anchorMin = screenPosAdjusted;
            indicatorArrow.anchorMax = screenPosAdjusted;
            indicatorArrow.anchoredPosition = Vector2.zero;
            indicatorArrow.localEulerAngles = new Vector3(0, 0, -90);
        }
        else
        {
            screenPos.x = Mathf.Clamp(screenPos.x, 0.05f, 0.95f);
            screenPos.y = Mathf.Clamp(screenPos.y, 0.05f, 0.95f);

            if (screenPos.z < 0)
            {
                screenPos.x = 1 - screenPos.x;
                screenPos.y = 1 - screenPos.y;
            }

            Vector3 screenCenter = new Vector3(0.5f, 0.5f, 0);
            Vector3 dir = (screenPos - screenCenter).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x);
            float slope = Mathf.Tan(angle);

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                screenPosAdjusted = new Vector3(
                    dir.x > 0 ? 1 : 0,
                    slope * (dir.x > 0 ? 0.5f : -0.5f) + screenCenter.y,
                    0
                );
            }
            else
            {
                screenPosAdjusted = new Vector3(
                    (0.5f / slope) * (dir.y > 0 ? 1 : -1) + screenCenter.x,
                    dir.y > 0 ? 1 : 0,
                    0
                );
            }

            screenPosAdjusted.x = Mathf.Clamp(screenPosAdjusted.x, 0f + edgePadding / Screen.width, 1f - edgePadding / Screen.width);
            screenPosAdjusted.y = Mathf.Clamp(screenPosAdjusted.y, 0f + edgePadding / Screen.height, 1f - edgePadding / Screen.height);

            indicatorArrow.anchorMin = screenPosAdjusted;
            indicatorArrow.anchorMax = screenPosAdjusted;
            indicatorArrow.anchoredPosition = Vector2.zero;

            float rotationAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            indicatorArrow.localEulerAngles = new Vector3(0, 0, rotationAngle);
        }
        
        float distanceFactor = Mathf.Clamp01((currentDistance - minDistance) / (maxDistance - minDistance));
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, distanceFactor);
        float scale = Mathf.Lerp(maxScale, minScale, distanceFactor);
        
        arrowImage.color = new Color(arrowImage.color.r, arrowImage.color.g, arrowImage.color.b, alpha);
        if (distanceText != null)
        {
            distanceText.color = new Color(distanceText.color.r, distanceText.color.g, distanceText.color.b, alpha);
        }
        indicatorArrow.localScale = new Vector3(scale, scale, 1);
    }
    
    void UpdatePulseEffect()
    {
        pulseTime += Time.deltaTime * pulseSpeed;
        float pulse = (Mathf.Sin(pulseTime) + 1) * 0.5f;
        
        Color pulseColor = Color.Lerp(Color.white, Color.yellow, pulse);
        arrowImage.color = new Color(
            pulseColor.r, 
            pulseColor.g, 
            pulseColor.b, 
            arrowImage.color.a
        );
    }
}