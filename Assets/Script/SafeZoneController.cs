/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using System.Collections;

public class SafeZoneController : MonoBehaviour
{
    [Header("Zone Settings")]
    public float initialWidth = 200f;       // 初始安全区宽度（X轴）
    public float finalWidth = 40f;          // 最终安全区宽度（X轴）
    public float length = 200f;             // 安全区长度（Z轴），固定不变
    public float height = 100f;             // 安全区高度（Y轴），固定不变
    public float shrinkDuration = 300f;     // 安全区缩小总时间(5分钟)
    public float damagePerSecond = 1f;      // 安全区外每秒伤害
    
    [Header("References")]
    public Transform safeZoneCenter;        // 安全区中心点
    public BoxCollider zoneCollider;        // 安全区碰撞体（改为BoxCollider）
    
    private float currentWidth;
    private float shrinkStartTime;
    private bool isShrinking;
    
    // 添加公共属性访问私有字段
    public float CurrentWidth => currentWidth;
    public bool IsShrinking => isShrinking;
    public float shrinkProgress { get; private set; } = 0f;
    
    void Start()
    {
        currentWidth = initialWidth;
        UpdateColliderSize();
        isShrinking = false;
        
        // 添加初始安全区视觉效果
        UpdateZoneVisual();
        
        // 设置初始安全区边界颜色
        SafeZoneBoundary boundary = FindObjectOfType<SafeZoneBoundary>();
        if (boundary != null)
        {
            boundary.SetBoundaryColor(Color.green);
        }
    }
    
    void Update()
    {
        if (isShrinking)
        {
            float elapsed = Time.time - shrinkStartTime;
            shrinkProgress = Mathf.Clamp01(elapsed / shrinkDuration);
            
            // 更新宽度
            currentWidth = Mathf.Lerp(initialWidth, finalWidth, shrinkProgress);
            UpdateColliderSize();
            UpdateZoneVisual();
            
            if (shrinkProgress >= 1f)
            {
                isShrinking = false;
            }
        }
    }
    public void ResetSafeZone()
    {
        currentWidth = initialWidth;
        shrinkStartTime = 0f;
        isShrinking = false;
        shrinkProgress = 0f;
        UpdateColliderSize();
        UpdateZoneVisual();
    }
        
    public void StartShrinking()
    {
        if (!isShrinking)
        {
            shrinkStartTime = Time.time;
            isShrinking = true;
            Debug.Log("安全区开始缩小!");
        }
    }
    
    // 更新碰撞体尺寸
    private void UpdateColliderSize()
    {
        if (zoneCollider != null)
        {
            zoneCollider.size = new Vector3(currentWidth, height, length);
        }
    }
    
    public bool IsPositionInSafeZone(Vector3 position)
    {
        if (safeZoneCenter == null || zoneCollider == null) return false;
        
        // 将位置转换到安全区的局部坐标系
        Vector3 localPos = safeZoneCenter.InverseTransformPoint(position);
        
        // 检查是否在长方体内
        return Mathf.Abs(localPos.x) <= currentWidth / 2f &&
               Mathf.Abs(localPos.y) <= height / 2f &&
               Mathf.Abs(localPos.z) <= length / 2f;
    }
    
    public float GetDamageAmount()
    {
        return damagePerSecond * Time.deltaTime;
    }
    
    private void UpdateZoneVisual()
    {
        // 更新安全区视觉效果，例如缩放一个平面或立方体
        // 注意：视觉效果物体应该是一个子物体，且其局部坐标系与安全区中心一致
        Transform visual = transform.Find("Visual");
        if (visual != null)
        {
            visual.localScale = new Vector3(currentWidth, 1f, length);
        }
    }
    
    // 在Scene视图中绘制安全区范围
    void OnDrawGizmosSelected()
    {
        if (safeZoneCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = safeZoneCenter.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(currentWidth, height, length));
        }
    }
}