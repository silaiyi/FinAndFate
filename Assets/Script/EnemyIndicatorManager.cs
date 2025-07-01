using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EnemyIndicatorManager : MonoBehaviour
{
    public static EnemyIndicatorManager Instance { get; private set; }

    [Header("UI References")]
    public RectTransform indicatorArrow;
    public Image arrowImage;
    public Text distanceText;

    [Header("Settings")]
    public float edgePadding = 50f;
    public float minDistance = 5f;
    public float maxDistance = 100f;
    public Color farColor = Color.white;
    public Color closeColor = Color.red;
    public float minScale = 0.5f;
    public float maxScale = 1.0f;
    public float minAlpha = 0.5f;
    public float maxAlpha = 1.0f;
    public Vector3 screenOffset = new Vector3(0, 0, 0);

    private Transform _playerTransform;
    private Transform _enemyTarget;
    private Camera _mainCamera;
    private bool _isActive = true;

    void Awake()
    {
        // 单例模式确保只有一个实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        _mainCamera = Camera.main;
        
        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Enemy indicators will be disabled.");
            _isActive = false;
        }
        
        // 初始时隐藏指示器
        SetIndicatorVisible(false);
    }

    void Update()
    {
        if (!_isActive || _enemyTarget == null) return;
        
        UpdateIndicator();
    }

    // 注册敌人目标
    public void RegisterEnemy(Transform enemyTransform)
    {
        _enemyTarget = enemyTransform;
        SetIndicatorVisible(true);
    }

    // 注销敌人目标
    public void UnregisterEnemy(Transform enemyTransform)
    {
        if (_enemyTarget == enemyTransform)
        {
            _enemyTarget = null;
            SetIndicatorVisible(false);
        }
    }

    // 更新指示器
    void UpdateIndicator()
    {
        if (_playerTransform == null || _enemyTarget == null) return;
        
        // 计算距离（在第三关忽略Y轴）
        float currentDistance;
        if (SceneManager.GetActiveScene().name == "Level3")
        {
            // 忽略Y轴，计算水平距离
            Vector2 playerPos = new Vector2(_playerTransform.position.x, _playerTransform.position.z);
            Vector2 enemyPos = new Vector2(_enemyTarget.position.x, _enemyTarget.position.z);
            currentDistance = Vector2.Distance(playerPos, enemyPos);
        }
        else
        {
            // 常规3D距离
            currentDistance = Vector3.Distance(_playerTransform.position, _enemyTarget.position);
        }
        
        // 更新距离文本
        if (distanceText != null)
        {
            distanceText.text = Mathf.RoundToInt(currentDistance) + "m";

            // 根据距离设置颜色（越近越红）
            distanceText.color = Color.Lerp(closeColor, farColor, (float)Mathf.InverseLerp(minDistance, maxDistance, currentDistance));
        }
        
        // 将敌人位置转换为屏幕坐标
        Vector3 screenPos = _mainCamera.WorldToViewportPoint(_enemyTarget.position);
        bool isTargetVisible = screenPos.z > 0 && screenPos.x > 0 && screenPos.x < 1 && screenPos.y > 0 && screenPos.y < 1;
        
        Vector3 screenPosAdjusted;
        
        if (isTargetVisible)
        {
            // 目标在屏幕内
            screenPosAdjusted = new Vector3(
                screenPos.x,
                screenPos.y + screenOffset.y,
                0
            );
            
            indicatorArrow.anchorMin = screenPosAdjusted;
            indicatorArrow.anchorMax = screenPosAdjusted;
            indicatorArrow.anchoredPosition = Vector2.zero;
            indicatorArrow.localEulerAngles = new Vector3(0, 0, -90);
            
            // 在屏幕内时隐藏箭头
            arrowImage.enabled = false;
        }
        else
        {
            // 目标在屏幕外
            arrowImage.enabled = true;
            
            // 调整屏幕位置确保在边缘内
            screenPos.x = Mathf.Clamp(screenPos.x, 0.05f, 0.95f);
            screenPos.y = Mathf.Clamp(screenPos.y, 0.05f, 0.95f);

            if (screenPos.z < 0)
            {
                // 如果敌人在相机后方，翻转位置
                screenPos.x = 1 - screenPos.x;
                screenPos.y = 1 - screenPos.y;
            }

            Vector3 screenCenter = new Vector3(0.5f, 0.5f, 0);
            Vector3 dir = (screenPos - screenCenter).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x);
            float slope = Mathf.Tan(angle);

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                // 水平方向优先
                screenPosAdjusted = new Vector3(
                    dir.x > 0 ? 1 : 0,
                    slope * (dir.x > 0 ? 0.5f : -0.5f) + screenCenter.y,
                    0
                );
            }
            else
            {
                // 垂直方向优先
                screenPosAdjusted = new Vector3(
                    (0.5f / slope) * (dir.y > 0 ? 1 : -1) + screenCenter.x,
                    dir.y > 0 ? 1 : 0,
                    0
                );
            }

            // 应用边缘填充
            screenPosAdjusted.x = Mathf.Clamp(screenPosAdjusted.x, 
                edgePadding / Screen.width, 
                1f - edgePadding / Screen.width);
                
            screenPosAdjusted.y = Mathf.Clamp(screenPosAdjusted.y, 
                edgePadding / Screen.height, 
                1f - edgePadding / Screen.height);

            indicatorArrow.anchorMin = screenPosAdjusted;
            indicatorArrow.anchorMax = screenPosAdjusted;
            indicatorArrow.anchoredPosition = Vector2.zero;

            // 旋转箭头指向敌人
            float rotationAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            indicatorArrow.localEulerAngles = new Vector3(0, 0, rotationAngle);
        }
        
        // 根据距离调整UI大小和透明度
        float distanceFactor = Mathf.Clamp01((currentDistance - minDistance) / (maxDistance - minDistance));
        float alpha = Mathf.Lerp(maxAlpha, minAlpha, distanceFactor);
        float scale = Mathf.Lerp(minScale, maxScale, distanceFactor);
        
        // 设置箭头颜色（越近越红）
        float colorLerp = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
        arrowImage.color = Color.Lerp(closeColor, farColor, colorLerp);
        arrowImage.color = new Color(arrowImage.color.r, arrowImage.color.g, arrowImage.color.b, alpha);
        
        if (distanceText != null)
        {
            distanceText.color = new Color(distanceText.color.r, distanceText.color.g, distanceText.color.b, alpha);
        }
        
        indicatorArrow.localScale = new Vector3(scale, scale, 1);
    }

    // 设置指示器可见性
    private void SetIndicatorVisible(bool visible)
    {
        if (indicatorArrow != null) indicatorArrow.gameObject.SetActive(visible);
        if (distanceText != null) distanceText.gameObject.SetActive(visible);
    }

    // 当关卡完成时调用
    public void OnLevelComplete(bool success)
    {
        _isActive = false;
        SetIndicatorVisible(false);
    }
}