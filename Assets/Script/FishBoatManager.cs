using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class FishBoatManager : MonoBehaviour
{
    public static FishBoatManager Instance { get; private set; }

    [Header("Boat Settings")]
    public GameObject fishingBoatPrefab, chasingBoatPrefab;
    public float boatSpacing = 30f;
    public float minDistanceBetweenBoats = 15f;

    private List<GameObject> activeBoats = new List<GameObject>();
    private List<Transform> fishRoadPoints = new List<Transform>();
    private Transform boatContainer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // 確保只在Level2/3場景運行
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Level2" && sceneName != "Level3") return;

        boatContainer = new GameObject("BoatContainer").transform;
        FindFishRoadPoints();
        UpdateBoatsBasedOnFishingScore();
        if (sceneName == "Level3")
        {
            AddChasingBoat();
        }
    }

    void FindFishRoadPoints()
    {
        GameObject[] roadObjects = GameObject.FindGameObjectsWithTag("FishRoad");
        foreach (GameObject road in roadObjects)
        {
            fishRoadPoints.Add(road.transform);
        }
        
        if (fishRoadPoints.Count == 0)
        {
            Debug.LogWarning("No FishRoad points found in the scene! Using default positions");
            CreateDefaultFishRoadPoints();
        }
    }

    void CreateDefaultFishRoadPoints()
    {
        // 創建默認路徑點
        for (int i = 0; i < 10; i++)
        {
            GameObject point = new GameObject($"DefaultFishRoad_{i}");
            point.tag = "FishRoad";
            point.transform.position = new Vector3(
                Random.Range(-100f, 100f),
                0,
                Random.Range(-100f, 100f)
            );
            fishRoadPoints.Add(point.transform);
        }
    }

    // 修改 UpdateBoatsBasedOnFishingScore 方法
    public void UpdateBoatsBasedOnFishingScore()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Level2" && sceneName != "Level3") return;
        
        int fishingScore = SwimmingController.Instance?.fishingScore ?? 0;
        int targetBoatCount = 0;

        // Level2 和 Level3 都生成普通渔船
        if (sceneName == "Level2")
        {
            if (fishingScore <= 4) targetBoatCount = 2;
            else if (fishingScore <= 9) targetBoatCount = 3;
            else targetBoatCount = 5;
        }
        else // Level3
        {
            // 修复：0-4分=4艘，5-9分=8艘，10分=12艘
            if (fishingScore <= 4) targetBoatCount = 4;
            else if (fishingScore <= 9) targetBoatCount = 8;
            else targetBoatCount = 12;
        }

        // 只统计普通渔船（排除追逐船）
        int currentFishingBoats = activeBoats.Count(b => 
            b != null && b.GetComponent<ChasingBoatController>() == null);

        while (currentFishingBoats > targetBoatCount)
        {
            GameObject boat = activeBoats.FirstOrDefault(b => 
                b != null && b.GetComponent<ChasingBoatController>() == null);
            if (boat) RemoveBoat(boat);
            currentFishingBoats--;
        }

        while (currentFishingBoats < targetBoatCount)
        {
            AddBoat();
            currentFishingBoats++;
        }
    }
    
    void AddChasingBoat()
    {
        if (chasingBoatPrefab == null)
        {
            Debug.LogError("Chasing boat prefab is not assigned!");
            return;
        }
        
        // 确保玩家存在
        if (SwimmingController.Instance == null)
        {
            Debug.LogWarning("Player not found, delaying chasing boat creation");
            StartCoroutine(DelayedAddChasingBoat());
            return;
        }
        
        // 在玩家附近生成 (确保至少50f距离)
        Vector3 playerPos = SwimmingController.Instance.transform.position;
        Vector3 spawnPosition;
        int attempts = 0;
        const float minDistance = 50f; // 最小距离
        const float maxDistance = 70f; // 最大距离
        
        do
        {
            // 生成在圆形区域（水平面上）
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistance, maxDistance);
            
            spawnPosition = playerPos + new Vector3(
                randomDir.x * distance,
                0, // 确保在水面上
                randomDir.y * distance
            );
            
            attempts++;
        } while (Vector3.Distance(spawnPosition, playerPos) < minDistance && attempts < 10);
        
        // 确保高度为0（水面）
        spawnPosition.y = 0;
        
        // 初始朝向玩家
        Quaternion spawnRotation = Quaternion.LookRotation(
            (playerPos - spawnPosition).normalized
        );
        
        GameObject chasingBoat = Instantiate(
            chasingBoatPrefab, 
            spawnPosition, 
            spawnRotation,
            boatContainer
        );
        
        ChasingBoatController chaser = chasingBoat.GetComponent<ChasingBoatController>();
        if (chaser != null)
        {
            chaser.playerTarget = SwimmingController.Instance.transform;
        }
        
        activeBoats.Add(chasingBoat);
        Debug.Log($"Chasing boat spawned at: {spawnPosition} " +
                $"(Distance: {Vector3.Distance(spawnPosition, playerPos):F1})");
    }
    
    // 延迟生成以防玩家未初始化
    IEnumerator DelayedAddChasingBoat()
    {
        yield return new WaitForSeconds(1f);
        
        if (SwimmingController.Instance != null)
        {
            AddChasingBoat();
        }
        else
        {
            Debug.LogError("Player still not found after delay!");
        }
    }

    void AddBoat()
    {
        if (fishingBoatPrefab == null)
        {
            Debug.LogError("Fishing boat prefab is not assigned!");
            return;
        }

        // 尋找合適的生成位置
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;
        bool foundPosition = false;
        int attempts = 0;

        while (!foundPosition && attempts < 50)
        {
            // 隨機選擇一個魚道點
            Transform randomPoint = fishRoadPoints[Random.Range(0, fishRoadPoints.Count)];
            spawnPosition = randomPoint.position;
            spawnRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // 檢查是否太靠近其他船
            bool tooClose = false;
            foreach (var boat in activeBoats)
            {
                if (boat != null && Vector3.Distance(spawnPosition, boat.transform.position) < minDistanceBetweenBoats)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                foundPosition = true;
            }

            attempts++;
        }

        GameObject newBoat = Instantiate(fishingBoatPrefab, spawnPosition, spawnRotation, boatContainer);
        FishingBoatController boatController = newBoat.GetComponent<FishingBoatController>();
        
        if (boatController != null)
        {
            // 設置漁船路徑點
            boatController.pathPoints = fishRoadPoints;
            boatController.moveSpeed = 8f + Random.Range(-1f, 1f); // 添加隨機速度變化
        }

        activeBoats.Add(newBoat);
    }

    void RemoveBoat(GameObject boat)
    {
        if (activeBoats.Contains(boat))
        {
            activeBoats.Remove(boat);
            Destroy(boat);
        }
    }

    public List<Transform> GetAllBoatTransforms()
    {
        List<Transform> boatTransforms = new List<Transform>();
        foreach (var boat in activeBoats)
        {
            if (boat != null)
            {
                boatTransforms.Add(boat.transform);
            }
        }
        return boatTransforms;
    }
}