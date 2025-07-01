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
    public float minDistanceBetweenBoats = 100f; // 间距增大到100f

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
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Level2" && sceneName != "Level3") return;
        
        int fishingScore = SwimmingController.Instance?.fishingScore ?? 0;
        int targetBoatCount = 0;

        if (sceneName == "Level2")
        {
            if (fishingScore <= 4) targetBoatCount = 2;
            else if (fishingScore <= 9) targetBoatCount = 3;
            else targetBoatCount = 5;
        }
        else
        {
            if (fishingScore <= 4) targetBoatCount = 4;
            else if (fishingScore <= 9) targetBoatCount = 8;
            else targetBoatCount = 12;
        }

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
        
        if (SwimmingController.Instance == null)
        {
            Debug.LogWarning("Player not found, delaying chasing boat creation");
            StartCoroutine(DelayedAddChasingBoat());
            return;
        }
        
        Vector3 playerPos = SwimmingController.Instance.transform.position;
        Vector3 spawnPosition;
        int attempts = 0;
        const float minDistance = 50f;
        const float maxDistance = 70f;
        
        do
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistance, maxDistance);
            
            spawnPosition = playerPos + new Vector3(
                randomDir.x * distance,
                0,
                randomDir.y * distance
            );
            
            attempts++;
        } while (Vector3.Distance(spawnPosition, playerPos) < minDistance && attempts < 10);
        
        spawnPosition.y = 0;
        
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

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;
        bool foundPosition = false;
        int attempts = 0;
        int maxAttempts = 100; // 增大尝试次数上限

        while (!foundPosition && attempts < maxAttempts)
        {
            Transform randomPoint = fishRoadPoints[Random.Range(0, fishRoadPoints.Count)];
            spawnPosition = randomPoint.position;
            spawnRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

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
            boatController.pathPoints = fishRoadPoints;
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