using UnityEngine;
using UnityEngine.UI;

public class DirectionIndicator : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform indicatorArrow; // 箭头UI元素
    public Image arrowImage; // 箭头图像组件
    public Text distanceText; // 距离文本组件

    [Header("Settings")]
    public float edgePadding = 50f; // 屏幕边缘内边距
    public float minDistance = 5f; // 最小距离
    public float maxDistance = 100f; // 最大距离
    public float minScale = 0.5f; // 最小缩放
    public float maxScale = 1.0f; // 最大缩放
    public float minAlpha = 0.5f; // 最小透明度
    public float maxAlpha = 1.0f; // 最大透明度
    public float pulseSpeed = 1.5f; // 脉动速度
    public Vector3 screenOffset = new Vector3(0, 0, 0); // 屏幕内位置偏移

    private Transform target; // 同伴的Transform
    private Camera mainCamera;
    private float pulseTime;
    [Header("Player Reference")]
    public Transform playerTransform; // 手动拖拽玩家对象到Inspector
    
    private bool _isActive = true; // 添加控制变量

    void Start()
    {
        mainCamera = Camera.main;
        pulseTime = 0f;
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // 优化目标获取逻辑
        FindCompanionTarget();
        // 在LevelManager中寻找同伴
        if (LevelManager.Instance != null && LevelManager.Instance.companionInstance != null)
        {
            target = LevelManager.Instance.companionInstance.transform;
        }
        else
        {
            // 如果LevelManager未初始化，尝试通过标签查找
            GameObject companion = GameObject.FindGameObjectWithTag("Companion");
            if (companion != null)
            {
                target = companion.transform;
            }
        }
        
        // 注册到LevelManager事件
        LevelManager.OnLevelComplete += HandleLevelComplete;
    }
    
    void OnDestroy()
    {
        // 取消事件注册
        LevelManager.OnLevelComplete -= HandleLevelComplete;
    }
    
    // 新增：处理关卡完成事件
    private void HandleLevelComplete(bool success)
    {
        _isActive = false; // 禁用指示器
        SetIndicatorVisible(false); // 立即隐藏UI
    }
    
    // 新增：设置指示器可见性
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
        // 如果指示器被禁用，直接返回
        if (!_isActive) return;
        
        if (target == null) 
        {
            // 如果目标丢失，尝试重新获取
            if (LevelManager.Instance != null && LevelManager.Instance.companionInstance != null)
            {
                target = LevelManager.Instance.companionInstance.transform;
            }
            return;
        }
        // 添加安全校验
        if (playerTransform == null) return;

        UpdateIndicator();
        UpdatePulseEffect();
    }

    void UpdateIndicator()
    {
        // 修复：使用玩家位置计算距离
        float currentDistance = Vector3.Distance(playerTransform.position, target.position);
        
        // 更新距离文本（确保组件引用）
        if (distanceText != null)
        {
            distanceText.text = Mathf.RoundToInt(currentDistance) + "m";
            // 调试输出
            //Debug.Log($"Distance updated: {currentDistance}m");
        }
        
        
        // 将目标位置转换为屏幕坐标
        Vector3 screenPos = mainCamera.WorldToViewportPoint(target.position);
        bool isTargetVisible = screenPos.z > 0 && screenPos.x > 0 && screenPos.x < 1 && screenPos.y > 0 && screenPos.y < 1;
        
        Vector3 screenPosAdjusted;
        
        if (isTargetVisible)
        {
            // 目标在屏幕内 - 直接显示在目标上方
            screenPosAdjusted = new Vector3(
                screenPos.x,
                screenPos.y + screenOffset.y, // 添加垂直偏移
                0
            );
            
            // 更新箭头位置
            indicatorArrow.anchorMin = screenPosAdjusted;
            indicatorArrow.anchorMax = screenPosAdjusted;
            indicatorArrow.anchoredPosition = Vector2.zero;
            
            // 箭头指向下方（表示目标在此位置）
            indicatorArrow.localEulerAngles = new Vector3(0, 0, -90);
        }
        else
        {
            // 目标在屏幕外 - 显示在屏幕边缘并指向目标方向
            
            // 调整屏幕坐标，使原点在中心
            screenPos.x = Mathf.Clamp(screenPos.x, 0.05f, 0.95f);
            screenPos.y = Mathf.Clamp(screenPos.y, 0.05f, 0.95f);

            // 如果目标在相机后方，反转坐标
            if (screenPos.z < 0)
            {
                screenPos.x = 1 - screenPos.x;
                screenPos.y = 1 - screenPos.y;
            }

            // 计算屏幕中心
            Vector3 screenCenter = new Vector3(0.5f, 0.5f, 0);

            // 计算从屏幕中心指向目标的方向
            Vector3 dir = (screenPos - screenCenter).normalized;

            // 计算方向与屏幕边缘的交点
            float angle = Mathf.Atan2(dir.y, dir.x);
            float slope = Mathf.Tan(angle);

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                // 左右边缘
                screenPosAdjusted = new Vector3(
                    dir.x > 0 ? 1 : 0,
                    slope * (dir.x > 0 ? 0.5f : -0.5f) + screenCenter.y,
                    0
                );
            }
            else
            {
                // 上下边缘
                screenPosAdjusted = new Vector3(
                    (0.5f / slope) * (dir.y > 0 ? 1 : -1) + screenCenter.x,
                    dir.y > 0 ? 1 : 0,
                    0
                );
            }

            // 应用边缘内边距
            screenPosAdjusted.x = Mathf.Clamp(screenPosAdjusted.x, 0f + edgePadding / Screen.width, 1f - edgePadding / Screen.width);
            screenPosAdjusted.y = Mathf.Clamp(screenPosAdjusted.y, 0f + edgePadding / Screen.height, 1f - edgePadding / Screen.height);

            // 更新箭头位置
            indicatorArrow.anchorMin = screenPosAdjusted;
            indicatorArrow.anchorMax = screenPosAdjusted;
            indicatorArrow.anchoredPosition = Vector2.zero;

            // 更新箭头旋转方向
            float rotationAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            indicatorArrow.localEulerAngles = new Vector3(0, 0, rotationAngle);
        }
        
        // 根据距离调整UI元素
        float distanceFactor = Mathf.Clamp01((currentDistance - minDistance) / (maxDistance - minDistance));
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, distanceFactor);
        float scale = Mathf.Lerp(maxScale, minScale, distanceFactor);
        
        // 设置UI元素的透明度和缩放
        arrowImage.color = new Color(arrowImage.color.r, arrowImage.color.g, arrowImage.color.b, alpha);
        if (distanceText != null)
        {
            distanceText.color = new Color(distanceText.color.r, distanceText.color.g, distanceText.color.b, alpha);
        }
        indicatorArrow.localScale = new Vector3(scale, scale, 1);
    }
    
    void UpdatePulseEffect()
    {
        // 脉动效果
        pulseTime += Time.deltaTime * pulseSpeed;
        float pulse = (Mathf.Sin(pulseTime) + 1) * 0.5f; // 0-1范围
        
        // 应用脉动效果到箭头颜色
        Color pulseColor = Color.Lerp(Color.white, Color.yellow, pulse);
        arrowImage.color = new Color(
            pulseColor.r, 
            pulseColor.g, 
            pulseColor.b, 
            arrowImage.color.a
        );
    }
}