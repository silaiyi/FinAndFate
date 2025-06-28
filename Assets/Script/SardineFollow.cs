using UnityEngine;

public class SardineFollow : MonoBehaviour
{
    public Transform leader; // 跟隨的領導者（玩家）
    public float followSpeed = 3f;
    public float rotationSpeed = 5f;
    
    [Header("Formation Settings")]
    public FormationType formationType;
    public float formationRadius = 2f;
    public float verticalOffset = 0.5f;
    public float positionRandomness = 0.3f;
    public float positionSmoothness = 0.1f;
    public float rotationRandomness = 5f;

    private Vector3 _targetPosition;
    private Vector3 _currentVelocity;
    private float _targetVerticalOffset;
    private float _formationAngle;
    private float _idleTimer;
    
    public enum FormationType
    {
        Flank,      // 側翼護衛
        Trailing,   // 後方跟隨
        Scouting    // 前方偵查
    }

    void Start()
    {
        if (leader == null) return;
        
        // 初始化隨機參數
        _targetVerticalOffset = Random.Range(-verticalOffset, verticalOffset);
        _formationAngle = Random.Range(0f, 360f);
        
        // 根據魚群類型設置初始位置
        switch (formationType)
        {
            case FormationType.Flank:
                _formationAngle = Random.Range(30f, 150f); // 側面角度
                break;
            case FormationType.Trailing:
                _formationAngle = Random.Range(150f, 210f); // 後方角度
                break;
            case FormationType.Scouting:
                _formationAngle = Random.Range(330f, 30f); // 前方角度
                break;
        }
    }

    void Update()
    {
        if (leader == null) return;

        // 計算基礎位置（玩家後方）
        Vector3 basePosition = leader.position - leader.forward * formationRadius;
        
        // 根據魚群類型計算目標位置
        switch (formationType)
        {
            case FormationType.Flank: // 側翼護衛
                _targetPosition = CalculateFlankPosition(basePosition);
                break;
                
            case FormationType.Trailing: // 後方跟隨
                _targetPosition = CalculateTrailingPosition(basePosition);
                break;
                
            case FormationType.Scouting: // 前方偵查
                _targetPosition = CalculateScoutingPosition(basePosition);
                break;
        }
        
        // 添加垂直偏移
        _targetPosition.y += _targetVerticalOffset;
        
        // 添加隨機移動效果
        ApplyIdleMovement();

        // 平滑移動到目標位置
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            _targetPosition, 
            ref _currentVelocity, 
            positionSmoothness
        );

        // 旋轉處理 - 面向移動方向
        if (_currentVelocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = _currentVelocity;
            lookDirection.y = 0; // 保持水平
            
            if (lookDirection != Vector3.zero)
            {
                // 添加隨機旋轉偏移
                Quaternion randomRotation = Quaternion.Euler(
                    0,
                    Random.Range(-rotationRandomness, rotationRandomness),
                    0
                );
                
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection) * randomRotation;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    Vector3 CalculateFlankPosition(Vector3 basePosition)
    {
        // 側翼位置：玩家左右兩側
        float angle = _formationAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * formationRadius,
            0,
            Mathf.Sin(angle) * formationRadius * 0.5f
        );
        
        return basePosition + offset;
    }

    Vector3 CalculateTrailingPosition(Vector3 basePosition)
    {
        // 後方位置：玩家後方扇形區域
        float angle = _formationAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * formationRadius * 0.7f,
            0,
            Mathf.Sin(angle) * formationRadius
        );
        
        return basePosition + offset;
    }

    Vector3 CalculateScoutingPosition(Vector3 basePosition)
    {
        // 前方位置：玩家前方扇形區域
        float angle = _formationAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * formationRadius * 0.8f,
            0,
            Mathf.Sin(angle) * formationRadius * 1.2f
        );
        
        return basePosition + offset;
    }

    void ApplyIdleMovement()
    {
        // 添加隨機的閒置移動效果
        _idleTimer += Time.deltaTime;
        
        if (_idleTimer > Random.Range(1f, 3f))
        {
            _targetVerticalOffset = Random.Range(-verticalOffset, verticalOffset);
            _formationAngle += Random.Range(-30f, 30f);
            _idleTimer = 0;
            
            // 添加小的隨機位置偏移
            _targetPosition += new Vector3(
                Random.Range(-positionRandomness, positionRandomness),
                Random.Range(-positionRandomness, positionRandomness),
                Random.Range(-positionRandomness, positionRandomness)
            );
        }
    }
}