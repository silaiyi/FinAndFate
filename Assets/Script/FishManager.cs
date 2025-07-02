/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using System.Collections.Generic;

public class FishManager : MonoBehaviour
{
    [Header("Fish Settings")]
    public GameObject[] fishPrefabs;
    public int maxFish = 100;
    public float spawnRadius = 30f;
    public float spawnInterval = 5f;
    
    private List<GameObject> allFish = new List<GameObject>();
    private float nextSpawnTime;
    private Transform player;
    
    void Start()
    {
        player = SwimmingController.Instance.transform;
        nextSpawnTime = Time.time + spawnInterval;
        
        // 初始生成一些鱼
        for (int i = 0; i < maxFish / 2; i++)
        {
            SpawnFish();
        }
    }
    
    void Update()
    {
        if (Time.time >= nextSpawnTime && allFish.Count < GetMaxFishAllowed())
        {
            SpawnFish();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    void SpawnFish()
    {
        if (fishPrefabs.Length == 0) 
        {
            Debug.LogError("No fish prefabs assigned!");
            return;
        }
        
        GameObject fishPrefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
        GameObject fish = Instantiate(fishPrefab, Vector3.zero, Quaternion.identity);
        fish.tag = "NPCFish";
        
        Vector3 spawnPos = FindSpawnPosition();
        fish.transform.position = spawnPos;
        fish.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        
        FishNPC fishNPC = fish.GetComponent<FishNPC>();
        if (fishNPC != null)
        {
            fishNPC.ResetFish();
            fishNPC.SetToxic(ShouldBeToxic());
        }
        
        allFish.Add(fish);
    }
    
    Vector3 FindSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        return player.position + 
               new Vector3(randomCircle.x, Random.Range(-5f, 5f), randomCircle.y);
    }
    
    bool ShouldBeToxic()
    {
        SwimmingController pollution = SwimmingController.Instance;
        if (pollution == null) return false;
        
        float toxicChance = (pollution.trashScore + pollution.sewageScore) * 0.05f;
        return Random.value < toxicChance;
    }
    
    int GetMaxFishAllowed()
    {
        SwimmingController pollution = SwimmingController.Instance;
        if (pollution == null) return maxFish;
        
        int reduction = (pollution.fishingScore + pollution.carbonScore) * 9;
        return Mathf.Max(10, maxFish - reduction);
    }
    
    // 清理所有鱼
    public void ClearAllFish()
    {
        foreach (GameObject fish in allFish)
        {
            if (fish != null)
            {
                Destroy(fish);
            }
        }
        allFish.Clear();
    }
}