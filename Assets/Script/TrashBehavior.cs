using UnityEngine;

public class TrashBehavior : MonoBehaviour
{
    public enum TrashLevel { Low, Medium, High, Hazardous, Extreme } // 1-5级
    
    [Header("Trash Settings")]
    public TrashLevel trashLevel = TrashLevel.Low;
    public int baseDamage = 5;
    public float despawnDistance = 200f;
    
    [Header("Extreme Settings")]
    public float poisonDamageInterval = 1f; // 中毒伤害间隔
    public float poisonDuration = 10f; // 毒素持续时间（秒）
    public float knockbackForce = 8f;
    
    private Transform player;
    private bool isExtremeActivated = false;
    private float nextDamageTime;
    private float poisonStartTime; // 毒素开始时间
    private float dangerLevel; // 危险度 (0-1)

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        SetupCollider();
    }
    
    void Update()
    {
        // 检查玩家距离，如果太远则删除垃圾
        if (player != null && Vector3.Distance(transform.position, player.position) > despawnDistance)
        {
            Destroy(gameObject);
            return;
        }
        
        // 5级垃圾的持续伤害效果
        if (isExtremeActivated)
        {
            // 检查毒素持续时间是否结束
            if (Time.time > poisonStartTime + poisonDuration)
            {
                // 持续时间结束，销毁垃圾
                Destroy(gameObject);
                return;
            }
            
            // 处理毒素伤害
            if (Time.time > nextDamageTime && player != null)
            {
                SwimmingController playerController = player.GetComponent<SwimmingController>();
                if (playerController != null)
                {
                    playerController.TakeDamage(1); // 每秒造成1点伤害
                    nextDamageTime = Time.time + poisonDamageInterval;
                }
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        SwimmingController playerController = other.GetComponent<SwimmingController>();
        if (playerController != null)
        {
            HandlePlayerCollision(playerController);
        }
    }
}

private void HandlePlayerCollision(SwimmingController playerController)
{
    Vector3 hitDirection = (playerController.transform.position - transform.position).normalized;
    
    switch (trashLevel)
    {
        case TrashLevel.Low: // 1级
        case TrashLevel.Medium: // 2级
        case TrashLevel.High: // 3级
            playerController.TakeDamage(baseDamage);
            Destroy(gameObject);
            break;
            
        case TrashLevel.Hazardous: // 4级
            playerController.TakeDamage(baseDamage * 2);
            // 只有4级垃圾减少最大生命值
            playerController.ReduceMaxHealth(5);
            ApplyKnockback(playerController, hitDirection, 15f);
            Destroy(gameObject);
            break;
            
        case TrashLevel.Extreme: // 5级
            playerController.TakeDamage(baseDamage * 4);
            // 只有5级垃圾减少最大生命值
            playerController.ReduceMaxHealth(10);
            ApplyKnockback(playerController, hitDirection, 7.5f);
            
            // 激活毒素效果
            isExtremeActivated = true;
            poisonStartTime = Time.time;
            nextDamageTime = Time.time + poisonDamageInterval;
            break;
    }
}
    
    private void ApplyKnockback(SwimmingController player, Vector3 direction, float force)
    {
        player.ApplyKnockback(direction * force * knockbackForce);
    }
    
    // 只设置碰撞器
    private void SetupCollider()
    {
        // 添加碰撞器（如果不存在）
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f; // 设置合适的碰撞半径
        }
        else
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
    /*
    // 设置垃圾危险度 (0-1)
    public void SetDangerLevel(float level)
    {
        dangerLevel = Mathf.Clamp01(level);
    }
    
    // 设置垃圾等级 (1-5)
    public void SetTrashLevel(int level)
    {
        level = Mathf.Clamp(level, 1, 5);
        trashLevel = (TrashLevel)(level - 1);
    }*/
}