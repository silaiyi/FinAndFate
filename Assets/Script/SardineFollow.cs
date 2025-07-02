/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SardineFollow : MonoBehaviour
{
    public Transform leader;
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;

    [Header("Formation Settings")]
    public float followRadius = 0.5f;
    public float positionRandomness = 0.07f;
    public float positionSmoothness = 0.15f;
    public float rotationRandomness = 5f;
    public float minSeparation = 0.2f;

    // 新增：三维位置设置
    private Vector3 _targetPosition;
    private Vector3 _currentVelocity;
    private float _idleTimer;
    private Vector3 _lastPosition;
    private static List<SardineFollow> _allSardines = new List<SardineFollow>();
    
    // 新增：球面坐标参数
    private float _horizontalAngle;
    private float _verticalAngle;
    private float _distanceFactor;
    private bool _angleInitialized = false;

    void Start()
    {
        if (leader == null) return;

        // 初始化球面坐标参数
        _horizontalAngle = Random.Range(0f, 360f);
        _verticalAngle = Random.Range(-30f, 30f); // 限制垂直角度范围
        _distanceFactor = Random.Range(0.8f, 1.2f);
        _angleInitialized = true;
        
        _lastPosition = transform.position;
        _allSardines.Add(this);
    }

    void OnDestroy()
    {
        _allSardines.Remove(this);
    }

    void Update()
    {
        if (leader == null) return;
        
        // 1. 计算基础位置（球面坐标）
        Vector3 basePosition = CalculateBasePosition();

        // 2. 添加随机偏移
        _targetPosition = basePosition + new Vector3(
            Random.Range(-positionRandomness, positionRandomness),
            Random.Range(-positionRandomness, positionRandomness),
            Random.Range(-positionRandomness, positionRandomness)
        );

        // 3. 添加群体分离行为
        ApplySeparationBehavior();

        // 4. 计算动态平滑系数
        float dynamicSmoothness = CalculateDynamicSmoothness();

        // 5. 平滑移动到目标位置
        transform.position = Vector3.SmoothDamp(
            transform.position,
            _targetPosition,
            ref _currentVelocity,
            dynamicSmoothness
        );

        // 6. 更新移动方向和旋转
        UpdateMovementDirection();
        
        // 7. 随机微调角度（模拟自然游动）
        ApplyIdleMovement();
    }

    // 使用球面坐标计算基础位置
    Vector3 CalculateBasePosition()
    {
        if (!_angleInitialized) return leader.position;
        
        // 将球面坐标转换为笛卡尔坐标
        float radH = _horizontalAngle * Mathf.Deg2Rad;
        float radV = _verticalAngle * Mathf.Deg2Rad;
        
        float x = Mathf.Cos(radV) * Mathf.Sin(radH);
        float y = Mathf.Sin(radV);
        float z = Mathf.Cos(radV) * Mathf.Cos(radH);
        
        Vector3 sphereOffset = new Vector3(x, y, z) * followRadius * _distanceFactor;
        
        // 转换到世界空间
        return leader.position + sphereOffset;
    }

    void ApplySeparationBehavior()
    {
        Vector3 separation = Vector3.zero;
        int count = 0;
        
        foreach (var other in _allSardines)
        {
            if (other == this) continue;
            
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance < minSeparation && distance > 0)
            {
                Vector3 dirAway = (transform.position - other.transform.position).normalized;
                separation += dirAway * (1.0f - distance / minSeparation);
                count++;
            }
        }
        
        if (count > 0)
        {
            separation /= count;
            _targetPosition += separation * 0.5f;
        }
    }

    // 借鉴掠食者的高效旋转逻辑
    void UpdateMovementDirection()
    {
        Vector3 moveDirection = (_targetPosition - transform.position).normalized;
        
        if (moveDirection != Vector3.zero)
        {
            // 使用掠食者风格的平滑旋转
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    float CalculateDynamicSmoothness()
    {
        float distanceToLeader = Vector3.Distance(transform.position, leader.position);
        float dynamicSmoothness = positionSmoothness;
        
        // 距离越远，移动越快（防止掉队）
        if (distanceToLeader > followRadius * 1.5f)
        {
            float speedBoost = Mathf.Clamp(1 + (distanceToLeader - followRadius), 1f, 3f);
            dynamicSmoothness = Mathf.Max(0.05f, positionSmoothness / speedBoost);
        }
        
        return dynamicSmoothness;
    }

    // 随机微调角度（模拟自然游动）
    void ApplyIdleMovement()
    {
        _idleTimer += Time.deltaTime;

        if (_idleTimer > Random.Range(0.8f, 2.0f))
        {
            // 小幅随机调整球面坐标
            _horizontalAngle += Random.Range(-10f, 10f);
            _verticalAngle = Mathf.Clamp(_verticalAngle + Random.Range(-5f, 5f), -30f, 30f);
            _distanceFactor = Mathf.Clamp(_distanceFactor + Random.Range(-0.1f, 0.1f), 0.7f, 1.3f);
            
            _idleTimer = 0;
        }
    }
    
    public void RemoveSardine()
    {
        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float duration = 0.5f;
        float elapsed = 0;
        Renderer renderer = GetComponent<Renderer>();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = Mathf.Lerp(1, 0, elapsed / duration);
                renderer.material.color = color;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}