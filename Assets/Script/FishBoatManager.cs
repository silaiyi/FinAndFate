using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FishBoatManager : MonoBehaviour
{
    public static FishBoatManager Instance { get; private set; }

    [Header("Boat Settings")]
    public GameObject fishingBoatPrefab;
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
        // 確保只在Level2場景運行
        if (SceneManager.GetActiveScene().name != "Level2") return;
        
        boatContainer = new GameObject("BoatContainer").transform;
        FindFishRoadPoints();
        UpdateBoatsBasedOnFishingScore();
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

    public void UpdateBoatsBasedOnFishingScore()
    {
        // 確保只在Level2場景運行
        if (SceneManager.GetActiveScene().name != "Level2") return;
        
        int fishingScore = SwimmingController.Instance != null ? 
            SwimmingController.Instance.fishingScore : 0;
        
        int targetBoatCount = 0;
        
        if (fishingScore <= 4) targetBoatCount = 2;
        else if (fishingScore <= 9) targetBoatCount = 3;
        else targetBoatCount = 5;
        
        // 調整船隻數量
        while (activeBoats.Count > targetBoatCount)
        {
            RemoveBoat(activeBoats[0]);
        }
        
        while (activeBoats.Count < targetBoatCount)
        {
            AddBoat();
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
                if (Vector3.Distance(spawnPosition, boat.transform.position) < minDistanceBetweenBoats)
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
            boatTransforms.Add(boat.transform);
        }
        return boatTransforms;
    }
}
