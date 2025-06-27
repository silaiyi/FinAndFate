using UnityEngine;

public class FoodBehavior : MonoBehaviour
{
    public int healAmount = 5; // 恢复的生命值
    public float despawnDistance = 200f; // 离开玩家多远后删除
    
    [Header("Movement Settings")]
    public float floatSpeed = 0.5f; // 上下浮动速度
    public float floatAmplitude = 1.5f; // 上下浮动幅度
    public float rotationSpeed = 30f; // 旋转速度
    public Vector3 rotationAxis = Vector3.up; // 旋转轴
    
    private Transform player;
    private Vector3 startPosition;
    private float randomOffset;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        SetupCollider();
        
        // 保存初始位置并添加随机偏移
        startPosition = transform.position;
        randomOffset = Random.Range(0f, 100f);
        
        // 设置随机旋转
        rotationAxis = new Vector3(
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f)
        ).normalized;
        
        // 随机缩放大小 (0.8-1.2倍)
        float scale = Random.Range(0.8f, 1.2f);
        transform.localScale = new Vector3(scale, scale, scale);
    }
    
    void Update()
    {
        // 离开玩家一定距离后删除
        if (player != null && Vector3.Distance(transform.position, player.position) > despawnDistance)
        {
            Destroy(gameObject);
            return;
        }
        
        // 更新浮动和旋转
        UpdateFloating();
        UpdateRotation();
    }
    
    void UpdateFloating()
    {
        // 使用正弦函数创建上下浮动效果
        float yOffset = Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatAmplitude;
        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + yOffset,
            startPosition.z
        );
    }
    
    void UpdateRotation()
    {
        // 随机旋转
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SwimmingController playerController = other.GetComponent<SwimmingController>();
            if (playerController != null)
            {
                playerController.Heal(healAmount);
                Destroy(gameObject);
            }
        }
    }
    
    private void SetupCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }
        else
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
}