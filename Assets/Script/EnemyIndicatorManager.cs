/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
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
    public float minAlpha = 0.3f; // 增加最小透明度，确保远距离可见
    public float maxAlpha = 1.0f;
    public Vector3 screenOffset = new Vector3(0, 0, 0);
    public float maxAngleOffset = 30f;
    
    [Header("Visibility Settings")]
    public float maxVisibleDistance = 500f; // 最大可见距离
    public float minVisibleScale = 0.2f; // 最小可见缩放
    public float minVisibleAlpha = 0.2f; // 最小可见透明度

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
        //SetIndicatorVisible(false);
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
        
        // 确保距离在合理范围内
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxVisibleDistance);
        
        // 更新距离文本
        if (distanceText != null)
        {
            distanceText.text = Mathf.RoundToInt(currentDistance) + "m";

            // 根据距离设置颜色（越近越红）
            distanceText.color = Color.Lerp(closeColor, farColor, (float)Mathf.InverseLerp(minDistance, maxDistance, currentDistance));
        }
        
        // 将敌人位置转换为屏幕坐标
        Vector3 screenPos = _mainCamera.WorldToViewportPoint(_enemyTarget.position);
        bool isBehindCamera = screenPos.z < 0;
        
        // 如果敌人在相机后方，翻转坐标
        if (isBehindCamera)
        {
            screenPos.x = 1 - screenPos.x;
            screenPos.y = 1 - screenPos.y;
        }
        
        // 计算从屏幕中心指向敌人屏幕位置的方向
        Vector3 screenCenter = new Vector3(0.5f, 0.5f, 0);
        Vector3 dir = (new Vector3(screenPos.x, screenPos.y, 0) - screenCenter);
        
        // 如果敌人在相机后方，方向取反
        if (isBehindCamera)
        {
            dir = -dir;
        }
        
        // 计算旋转角度
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        // 添加随机角度偏移，使箭头更明显
        float angleOffset = Mathf.Sin(Time.time * 3f) * maxAngleOffset;
        angle += angleOffset;
        
        // 计算边缘位置
        Vector3 edgePosition = CalculateEdgePosition(screenCenter, dir);
        
        // 设置箭头位置
        indicatorArrow.anchorMin = edgePosition;
        indicatorArrow.anchorMax = edgePosition;
        indicatorArrow.anchoredPosition = Vector2.zero;
        
        // 设置箭头旋转
        indicatorArrow.localEulerAngles = new Vector3(0, 0, angle);
        
        // 根据距离调整UI大小和透明度
        // 使用非线性曲线确保远距离时仍可见
        float distanceFactor = Mathf.Clamp01((currentDistance - minDistance) / (maxVisibleDistance - minDistance));
        
        // 使用平方函数使远距离变化更平缓
        float visibilityFactor = 1 - Mathf.Pow(distanceFactor, 0.5f);
        
        float alpha = Mathf.Lerp(minVisibleAlpha, maxAlpha, visibilityFactor);
        float uiScale = Mathf.Lerp(minVisibleScale, maxScale, visibilityFactor);
        
        // 设置箭头颜色（越近越红）
        float colorLerp = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
        arrowImage.color = Color.Lerp(closeColor, farColor, colorLerp);
        arrowImage.color = new Color(arrowImage.color.r, arrowImage.color.g, arrowImage.color.b, alpha);
        
        if (distanceText != null)
        {
            distanceText.color = new Color(distanceText.color.r, distanceText.color.g, distanceText.color.b, alpha);
        }
        
        indicatorArrow.localScale = new Vector3(uiScale, uiScale, 1);
    }
    
    // 计算边缘位置
    Vector3 CalculateEdgePosition(Vector3 screenCenter, Vector3 dir)
    {
        // 如果方向向量长度接近0（敌人在屏幕中心），则使用默认方向
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector3.right;
        }
        
        Vector3 dirNormalized = dir.normalized;
        
        // 计算缩放比例，使点位于屏幕边缘
        float scaleX = (dirNormalized.x > 0 ? (1 - screenCenter.x) : screenCenter.x) / Mathf.Abs(dirNormalized.x);
        float scaleY = (dirNormalized.y > 0 ? (1 - screenCenter.y) : screenCenter.y) / Mathf.Abs(dirNormalized.y);
        float scale = Mathf.Min(scaleX, scaleY);
        
        // 计算边缘位置
        Vector3 edgePosition = screenCenter + dirNormalized * scale * 0.9f; // 稍微向内缩进
        
        // 应用边缘填充
        edgePosition.x = Mathf.Clamp(edgePosition.x, 
            edgePadding / Screen.width, 
            1f - edgePadding / Screen.width);
            
        edgePosition.y = Mathf.Clamp(edgePosition.y, 
            edgePadding / Screen.height, 
            1f - edgePadding / Screen.height);
            
        return edgePosition;
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