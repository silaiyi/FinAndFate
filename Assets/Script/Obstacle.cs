/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Disappearance Settings")]
    public float disappearanceCheckInterval = 10f; // 消失檢查間隔
    public float disappearanceChance = 0.1f; // 每次檢查消失機率
    public float fadeDuration = 1.5f; // 淡出持續時間
    private float nextDisappearanceCheckTime;
    private bool isDisappearing = false;

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
            originalMaterials.Add(new Material(renderer.material)); // 创建材质副本
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

            //Debug.Log($"{gameObject.name} coral created with {currentHealth}/{maxHealthValue} health");

            // 初始化消失检查时间
            nextDisappearanceCheckTime = Time.time + disappearanceCheckInterval;
        }

        disappearanceCheckInterval = Random.Range(0f, 1f);
        nextDisappearanceCheckTime = Time.time + disappearanceCheckInterval;
    }

    void Update()
    {
        // 仅珊瑚需要消失检查
        if (obstacleType != ObstacleType.Coral || isBleached || isDisappearing) 
            return;
        
        // 检查是否到达消失检查时间
        if (Time.time >= nextDisappearanceCheckTime)
        {
            nextDisappearanceCheckTime = Time.time + disappearanceCheckInterval;
            CheckForDisappearance();
        }
    }
    
    // 替换原有的CheckForDisappearance方法
    void CheckForDisappearance()
    {
        // 獲取當前捕撈分數
        int fishingScore = SwimmingController.Instance != null ? 
            SwimmingController.Instance.fishingScore : 0;
        
        // 新的概率計算：捕撈分數0分時0%消失率，10分時90%消失率
        float adjustedChance = Mathf.Clamp01(fishingScore / 10.0f) * 0.9f;
        
        // 隨機決定是否消失
        if (Random.value < adjustedChance)
        {
            Debug.Log($"{gameObject.name} 因捕捞分数{fishingScore}消失 (概率: {adjustedChance*100}%)");
            StartCoroutine(FadeOutAndDestroy());
        }
    }
    
    IEnumerator FadeOutAndDestroy()
    {
        isDisappearing = true;
        
        // 更改标签，避免被重复选中
        gameObject.tag = "Untagged";
        
        // 禁用碰撞器
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // 淡出過程
        float timer = 0f;
        
        // 儲存所有渲染器的原始顏色
        List<Color[]> originalColorsList = new List<Color[]>();
        foreach (Renderer renderer in childRenderers)
        {
            Material[] materials = renderer.materials;
            Color[] colors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                colors[i] = materials[i].color;
            }
            originalColorsList.Add(colors);
        }

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);

            // 對所有子物件進行透明度變化
            for (int i = 0; i < childRenderers.Count; i++)
            {
                Renderer renderer = childRenderers[i];
                Material[] materials = renderer.materials;
                Color[] originalColors = originalColorsList[i];

                for (int j = 0; j < materials.Length; j++)
                {
                    if (j < originalColors.Length)
                    {
                        Color newColor = originalColors[j];
                        newColor.a = Mathf.Lerp(1f, 0f, progress);
                        materials[j].color = newColor;
                    }
                }
            }

            yield return null;
        }

        // 銷毀物體
        Destroy(gameObject);
    }

    // 接收伤害
    public void TakeDamage(int damage)
    {
        if (obstacleType != ObstacleType.Coral || isBleached || isDisappearing) return;

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

        // 儲存所有渲染器的原始顏色
        List<Color[]> originalColorsList = new List<Color[]>();
        foreach (Renderer renderer in childRenderers)
        {
            Material[] materials = renderer.materials;
            Color[] colors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                colors[i] = materials[i].color;
            }
            originalColorsList.Add(colors);
        }

        // 白化過程
        float timer = 0f;

        while (timer < bleachingDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / bleachingDuration);

            // 對所有子物件進行顏色漸變
            for (int i = 0; i < childRenderers.Count; i++)
            {
                Renderer renderer = childRenderers[i];
                Material[] materials = renderer.materials;
                Color[] originalColors = originalColorsList[i];

                for (int j = 0; j < materials.Length; j++)
                {
                    if (j < originalColors.Length)
                    {
                        // 計算新的顏色 (逐漸變白)
                        Color newColor = Color.Lerp(
                            originalColors[j], 
                            Color.white, 
                            progress
                        );
                        materials[j].color = newColor;
                    }
                }
            }

            yield return null;
        }

        // 標記為已白化
        isBleached = true;
        Debug.Log($"{gameObject.name} coral fully bleached");
    }
    
    void OnDestroy()
    {
        
    }
}