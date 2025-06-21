using UnityEngine;
using System.Collections;

public class Obstacle : MonoBehaviour
{
    public enum ObstacleType { Coral, Rock }
    public ObstacleType obstacleType;
    
    [Header("Health Settings (Coral Only)")]
    public int minHealth = 200;
    public int maxHealth = 1000;
    public int currentHealth;
    private int maxHealthValue;
    
    [Header("Visual Effects")]
    public Material damagedMaterial; // 受伤材质
    public Material coralMaterial; // 珊瑚的原始材质
    private Material originalMaterial;
    private Renderer obstacleRenderer;
    
    [Header("Particle Effects")]
    public GameObject coralDestroyEffectPrefab; // 摧毁粒子效果预制体
    public ParticleSystem damageParticles; // 受伤粒子效果
    
    [Header("Debris Settings")]
    public GameObject debrisPrefab; // 碎片预制体
    public int minDebris = 3;
    public int maxDebris = 8;
    
    void Start()
    {
        // 设置标签
        gameObject.tag = obstacleType == ObstacleType.Coral ? 
            "CoralObstacle" : "RockObstacle";
        
        // 初始化珊瑚生命值
        if (obstacleType == ObstacleType.Coral)
        {
            maxHealthValue = Random.Range(minHealth, maxHealth + 1);
            currentHealth = maxHealthValue;
            
            // 获取渲染器和材质
            obstacleRenderer = GetComponent<Renderer>();
            if (obstacleRenderer != null)
            {
                originalMaterial = obstacleRenderer.material;
                
                // 如果未指定珊瑚材质，使用原始材质
                if (coralMaterial == null) 
                {
                    coralMaterial = originalMaterial;
                }
            }
            
            // 初始化受伤粒子效果
            if (damageParticles == null)
            {
                damageParticles = GetComponentInChildren<ParticleSystem>();
            }
            
            Debug.Log($"{gameObject.name} coral created with {currentHealth}/{maxHealthValue} health");
        }
    }
    
    // 接收伤害
    public void TakeDamage(int damage)
    {
        if (obstacleType != ObstacleType.Coral) return;
        
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealthValue}");
        
        // 应用伤害效果
        StartCoroutine(ShowDamageEffect());
        
        // 检查是否被摧毁
        if (currentHealth <= 0)
        {
            DestroyObstacle();
        }
    }
    
    // 显示伤害效果
    IEnumerator ShowDamageEffect()
    {
        // 播放粒子效果
        if (damageParticles != null)
        {
            damageParticles.Play();
        }
        
        // 材质变化效果
        if (obstacleRenderer != null && damagedMaterial != null)
        {
            obstacleRenderer.material = damagedMaterial;
            yield return new WaitForSeconds(0.3f);
            obstacleRenderer.material = originalMaterial;
        }
        else
        {
            yield return null;
        }
    }
    
    // 摧毁障碍物
    public void DestroyObstacle()
    {
        Debug.Log($"{gameObject.name} coral destroyed");
        
        // 播放摧毁粒子效果
        PlayDestroyEffect();
        
        // 生成碎片
        CreateCoralDebris();
        
        // 禁用碰撞器
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // 隐藏渲染器
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        
        // 如果有其他组件也需要禁用
        Behaviour[] components = GetComponents<Behaviour>();
        foreach (Behaviour component in components)
        {
            if (component != this && component.enabled)
            {
                component.enabled = false;
            }
        }
        
        // 延迟销毁
        Destroy(gameObject, 3f);
    }
    
    // 播放摧毁粒子效果
    void PlayDestroyEffect()
    {
        if (coralDestroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                coralDestroyEffectPrefab, 
                transform.position, 
                Quaternion.identity
            );
            
            // 配置粒子效果
            CoralParticleController controller = effect.GetComponent<CoralParticleController>();
            if (controller != null && coralMaterial != null)
            {
                controller.PlayEffects(transform.position, coralMaterial);
            }
            else
            {
                // 如果没有控制器，5秒后销毁效果
                Destroy(effect, 5f);
            }
        }
    }
    
    // 创建珊瑚碎片
    void CreateCoralDebris()
    {
        if (debrisPrefab == null) return;
        
        int debrisCount = Random.Range(minDebris, maxDebris + 1);
        for (int i = 0; i < debrisCount; i++)
        {
            // 创建碎片实例
            GameObject debris = Instantiate(
                debrisPrefab, 
                transform.position + Random.insideUnitSphere * 0.5f, 
                Random.rotation
            );
            
            // 设置碎片材质
            Renderer debrisRenderer = debris.GetComponent<Renderer>();
            if (debrisRenderer != null && coralMaterial != null)
            {
                debrisRenderer.material = coralMaterial;
            }
            
            // 添加物理效果
            Rigidbody rb = debris.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = debris.AddComponent<Rigidbody>();
            }
            
            // 添加随机力
            Vector3 force = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(1f, 5f),
                Random.Range(-5f, 5f)
            );
            
            rb.AddForce(force, ForceMode.Impulse);
            
            // 添加随机扭矩
            Vector3 torque = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );
            
            rb.AddTorque(torque, ForceMode.Impulse);
            
            // 延迟销毁碎片
            Destroy(debris, Random.Range(2f, 5f));
        }
    }
}