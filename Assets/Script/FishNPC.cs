using UnityEngine; 
using System.Collections.Generic; 

public class FishNPC : MonoBehaviour 
{ 
    [Header("Movement Settings")] 
    public float swimSpeed = 8f; 
    public float rotationSpeed = 5f; 
    public float waypointThreshold = 2f; 
    public float followChance = 0.3f; 
    
    [Header("Toxic Settings")] 
    public bool isToxic = false; 

    [Header("Spacing Settings")] 
    public float minFishDistance = 5f; 
    public float avoidanceForce = 2f;   
    
    private int currentPointIndex = 0; 
    private Transform leaderFish; 
    private List<Transform> pathPoints = new List<Transform>(); 

    void Start() 
    { 
        // 直接查找所有"FishSwim"路径点
        FindPathPoints();
        gameObject.tag = "NPCFish";
        
        // 随机选择起始点 
        if (pathPoints.Count > 0)
        {
            currentPointIndex = Random.Range(0, pathPoints.Count);
            transform.position = pathPoints[currentPointIndex].position;
        }
    } 
    
    void FindPathPoints()
    {
        GameObject[] roadObjects = GameObject.FindGameObjectsWithTag("FishSwim"); 
        foreach (var go in roadObjects) 
        { 
            pathPoints.Add(go.transform); 
        }
    }

    void Update() 
    { 
        // 定期尝试寻找领头鱼
        if (leaderFish == null && Random.value < followChance * Time.deltaTime) 
        { 
            FindLeaderToFollow(); 
        } 

        if (leaderFish != null) 
        { 
            FollowLeader(); 
        } 
        else 
        { 
            FollowPath(); 
        } 
    } 

    // 保持鱼群间距的方法
    void MaintainDistance()
    {
        Collider[] nearbyFish = Physics.OverlapSphere(transform.position, minFishDistance); 
        foreach (Collider fish in nearbyFish)
        {
            if (fish != null && fish.gameObject != gameObject && fish.CompareTag("NPCFish"))
            {
                Vector3 awayDirection = (transform.position - fish.transform.position).normalized;
                float distance = Vector3.Distance(transform.position, fish.transform.position);
                
                // 距离越近，避让力度越大
                float forceMultiplier = Mathf.Clamp01(1 - (distance / minFishDistance));
                transform.position += awayDirection * avoidanceForce * forceMultiplier * Time.deltaTime;
            }
        }
    }

    void FollowPath() 
    { 
        if (pathPoints.Count == 0) 
        {
            // 如果路径点列表为空，重新查找
            FindPathPoints();
            if (pathPoints.Count == 0) return;
        }

        MaintainDistance(); // 保持间距
        
        Vector3 targetPosition = pathPoints[currentPointIndex].position; 
        Vector3 moveDirection = (targetPosition - transform.position).normalized; 

        if (moveDirection != Vector3.zero) 
        { 
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection); 
            transform.rotation = Quaternion.Slerp( 
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime 
            ); 

            transform.Translate( 
                Vector3.forward * swimSpeed * Time.deltaTime, 
                Space.Self 
            ); 
        } 

        if (Vector3.Distance(transform.position, targetPosition) < waypointThreshold) 
        { 
            SelectNextWaypoint(); 
        } 
    } 

    void SelectNextWaypoint() 
    { 
        if (pathPoints.Count <= 1) return; 
        
        int newIndex; 
        do { 
            newIndex = Random.Range(0, pathPoints.Count); 
        } while (newIndex == currentPointIndex); 
        
        currentPointIndex = newIndex; 
    } 

    void FindLeaderToFollow() 
    { 
        Collider[] nearbyFish = Physics.OverlapSphere(transform.position, 5f); 
        foreach (Collider fish in nearbyFish) 
        { 
            if (fish != null &&  
                fish.gameObject != gameObject && 
                fish.CompareTag("NPCFish")) 
            { 
                FishNPC otherFish = fish.GetComponent<FishNPC>(); 
                if (otherFish != null && otherFish.leaderFish == null) 
                { 
                    leaderFish = fish.transform; 
                    return; 
                } 
            } 
        } 
    } 

    void FollowLeader() 
    { 
        if (leaderFish == null) return; 

        MaintainDistance(); // 保持间距
        
        Vector3 followDirection = (leaderFish.position - transform.position).normalized; 
        Quaternion targetRotation = Quaternion.LookRotation(followDirection); 
        transform.rotation = Quaternion.Slerp( 
            transform.rotation,  
            targetRotation,  
            rotationSpeed * 2 * Time.deltaTime 
        ); 
        
        transform.Translate( 
            Vector3.forward * swimSpeed * 1.2f * Time.deltaTime, 
            Space.Self 
        ); 

        // 随机停止跟随 
        if (Random.value < 0.01f ||  
            Vector3.Distance(transform.position, leaderFish.position) > 8f) 
        { 
            leaderFish = null; 
        } 
    } 

    public void SetToxic(bool toxic) => isToxic = toxic; 

    public void ResetFish() 
    { 
        leaderFish = null; 
        isToxic = false; 
    } 
}