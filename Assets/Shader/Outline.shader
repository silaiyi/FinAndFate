using UnityEngine;
using System.Collections;
using System.Collections.Generic; // 添加命名空間用於List

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
    private List<Material> originalMaterials; // 儲存所有原始材質
    private List<Renderer> childRenderers; // 儲存所有子物件的Renderer

    [Header("Particle Effects")]
    public GameObject coralDestroyEffectPrefab; // 摧毁粒子效果预制体
    public ParticleSystem damageParticles; // 受伤粒子效果

    [Header("Debris Settings")]
    public GameObject debrisPrefab; // 碎片预制体
    public int minDebris = 3;
    public int maxDebris = 8;
    
    [Header("Bleaching Settings")]
    public Material bleachedMaterial; // 白化材質
    public float bleachingDuration = 5f; // 白化過程持續時間
    private bool isBleached = false; // 是否已白化

    void Start()
    {
        // 设置标签
        gameObject.tag = obstacleType == ObstacleType.Coral ?
            "CoralObstacle" : "RockObstacle";

        // 初始化渲染器列表
        childRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
        originalMaterials = new List<Material>();

        // 儲存所有子物件的原始材質
        foreach (Renderer renderer in childRenderers)
        {
            originalMaterials.Add(renderer.material);
        }

        // 初始化珊瑚生命值
        if (obstacleType == ObstacleType.Coral)
        {
            maxHealthValue = Random.Range(minHealth, maxHealth + 1);
            currentHealth = maxHealthValue;

            // 如果未指定珊瑚材质，使用第一个子物件的材质
            if (coralMaterial == null && childRenderers.Count > 0)
            {
                coralMaterial = childRenderers[0].material;
            }

            // 初始化受伤粒子效果
            if (damageParticles == null)
            {
                damageParticles = GetComponentInChildren<ParticleSystem>();
            }

            Debug.Log($"{gameObject.name} coral created with {currentHealth}/{maxHealthValue} health");
        }
        
        if (OutlineManager.Instance != null)
        {
            Color outlineColor = obstacleType == ObstacleType.Coral ?
                OutlineManager.Instance.coralOutlineColor :
                OutlineManager.Instance.rockOutlineColor;

            OutlineManager.Instance.ApplyOutline(gameObject, outlineColor);
        }
    }

    // 接收伤害
    public void TakeDamage(int damage)
    {
        if (obstacleType != ObstacleType.Coral || isBleached) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            StartCoroutine(BleachCoral());
        }
        else
        {
            StartCoroutine(ShowDamageEffect());
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
        if (childRenderers != null && childRenderers.Count > 0 && damagedMaterial != null)
        {
            // 應用受傷材質到所有子物件
            foreach (Renderer renderer in childRenderers)
            {
                renderer.material = damagedMaterial;
            }
            
            yield return new WaitForSeconds(0.3f);
            
            // 恢復原始材質
            for (int i = 0; i < childRenderers.Count; i++)
            {
                if (i < originalMaterials.Count)
                {
                    childRenderers[i].material = originalMaterials[i];
                }
            }
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

        // 隐藏所有渲染器
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = false;
        }

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
    
    IEnumerator BleachCoral()
    {
        Debug.Log($"{gameObject.name} coral bleaching started");

        // 更改標籤，失去躲藏功能
        gameObject.tag = "BleachedCoral";

        // 禁用碰撞器
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 儲存當前材質
        List<Material> startMaterials = new List<Material>();
        foreach (Renderer renderer in childRenderers)
        {
            startMaterials.Add(renderer.material);
        }

        // 白化過程
        float timer = 0f;

        while (timer < bleachingDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / bleachingDuration;

            // 對所有子物件進行材質漸變
            for (int i = 0; i < childRenderers.Count; i++)
            {
                if (i < startMaterials.Count && bleachedMaterial != null)
                {
                    // 創建過渡材質
                    Material lerpedMaterial = new Material(startMaterials[i]);
                    lerpedMaterial.Lerp(startMaterials[i], bleachedMaterial, progress);
                    childRenderers[i].material = lerpedMaterial;
                }
            }

            yield return null;
        }

        // 應用最終白化材質
        foreach (Renderer renderer in childRenderers)
        {
            if (bleachedMaterial != null)
            {
                renderer.material = bleachedMaterial;
            }
        }

        // 標記為已白化
        isBleached = true;
        Debug.Log($"{gameObject.name} coral fully bleached");
    }
    
    void OnDestroy()
    {
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.RemoveOutline(gameObject);
        }
    }
}