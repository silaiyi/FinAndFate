using UnityEngine;

public class TrashBehavior : MonoBehaviour
{
    public enum TrashLevel { Low, Medium, High, Hazardous, Extreme }
    
    [Header("Trash Settings")]
    public TrashLevel trashLevel = TrashLevel.Low;
    public int baseDamage = 5;
    public float despawnDistance = 200f;
    public float poisonDamageInterval = 1f;
    public float poisonDuration = 10f;
    public float knockbackForce = 8f;
    
    private Transform player;
    private bool isExtremeActivated;
    private float nextDamageTime;
    private float poisonStartTime;
    [Header("Height Restrictions")]
    public float minHeight = -100f; // 最低高度
    public float maxHeight = 0f;   // 最高高度

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        SetupCollider();
        CheckHeightValidity();
    }
    
    void Update()
    {
        // 新增：持续检查高度是否在有效范围内
        if (!IsWithinValidHeight())
        {
            Destroy(gameObject);
            return;
        }
        if (player != null && Vector3.Distance(transform.position, player.position) > despawnDistance)
        {
            Destroy(gameObject);
            return;
        }
        
        if (isExtremeActivated && Time.time > poisonStartTime + poisonDuration)
        {
            Destroy(gameObject);
            return;
        }
        
        if (isExtremeActivated && Time.time > nextDamageTime && player != null)
        {
            SwimmingController playerController = player.GetComponent<SwimmingController>();
            if (playerController != null)
            {
                playerController.TakeDamage(1);
                nextDamageTime = Time.time + poisonDamageInterval;
            }
        }
    }
    // 新增：检查高度是否在有效范围内
    private bool IsWithinValidHeight()
    {
        return transform.position.y >= minHeight && transform.position.y <= maxHeight;
    }
    
    // 新增：检查并处理高度有效性
    private void CheckHeightValidity()
    {
        if (!IsWithinValidHeight())
        {
            Debug.Log($"Destroying trash at invalid height: {transform.position.y}");
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SwimmingController playerController = other.GetComponent<SwimmingController>();
            if (playerController != null) HandlePlayerCollision(playerController);
        }
    }

    private void HandlePlayerCollision(SwimmingController playerController)
    {
        Vector3 hitDirection = (playerController.transform.position - transform.position).normalized;
        
        switch (trashLevel)
        {
            case TrashLevel.Low:
            case TrashLevel.Medium:
            case TrashLevel.High:
                playerController.TakeDamage(baseDamage);
                Destroy(gameObject);
                break;
                
            case TrashLevel.Hazardous:
                playerController.TakeDamage(baseDamage * 2);
                playerController.ReduceMaxHealth(5);
                ApplyKnockback(playerController, hitDirection, 15f);
                Destroy(gameObject);
                break;
                
            case TrashLevel.Extreme:
                playerController.TakeDamage(baseDamage * 4);
                playerController.ReduceMaxHealth(10);
                ApplyKnockback(playerController, hitDirection, 7.5f);
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