/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using System.Collections; // 添加 System.Collections 命名空间
using System.Collections.Generic;

public class CoralPresenceController : MonoBehaviour
{
    public static CoralPresenceController Instance;
    
    [Header("Coral Settings")]
    public float checkInterval = 10f; // 檢查間隔
    public List<GameObject> allCorals = new List<GameObject>(); // 所有珊瑚列表
    
    private float nextCheckTime;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 獲取場景中所有珊瑚
        GameObject[] coralArray = GameObject.FindGameObjectsWithTag("CoralObstacle");
        allCorals = new List<GameObject>(coralArray);
        
        nextCheckTime = Time.time + checkInterval;
    }
    
    void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            UpdateCoralPresence();
        }
    }
    
    void UpdateCoralPresence()
    {
        if (allCorals.Count == 0) return;
        
        // 獲取當前捕撈分數
        int fishingScore = SwimmingController.Instance != null ? SwimmingController.Instance.fishingScore : 0;
        
        // 計算移除機率 (分數越高移除機率越大)
        float removeChance = Mathf.Clamp01(fishingScore / 10f);
        
        List<GameObject> coralsToRemove = new List<GameObject>();
        
        foreach (GameObject coral in allCorals)
        {
            if (coral == null) continue;
            
            // 隨機決定是否移除
            if (Random.value < removeChance)
            {
                coralsToRemove.Add(coral);
            }
        }
        
        // 移除珊瑚
        foreach (GameObject coral in coralsToRemove)
        {
            // 快速淡出效果
            StartCoroutine(FadeOutAndDestroy(coral));
            allCorals.Remove(coral);
        }
    }
    
    IEnumerator FadeOutAndDestroy(GameObject coral)
    {
        Renderer renderer = coral.GetComponent<Renderer>();
        if (renderer == null) yield break;
        
        Material[] materials = renderer.materials;
        Color[] originalColors = new Color[materials.Length];
        
        // 保存原始顏色
        for (int i = 0; i < materials.Length; i++)
        {
            originalColors[i] = materials[i].color;
        }
        
        // 淡出過程
        float duration = 1.5f;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            
            for (int i = 0; i < materials.Length; i++)
            {
                Color newColor = originalColors[i];
                newColor.a = Mathf.Lerp(1f, 0f, progress);
                materials[i].color = newColor;
            }
            
            yield return null;
        }
        
        // 銷毀物體
        Destroy(coral);
    }
    
    // 註冊新珊瑚（在珊瑚生成時調用）
    public void RegisterCoral(GameObject coral)
    {
        if (!allCorals.Contains(coral))
        {
            allCorals.Add(coral);
        }
    }
}