/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;
    
    [Header("Checkpoint Settings")]
    public List<Transform> checkpoints = new List<Transform>();
    public int currentCheckpointIndex = 0;
    public float checkpointActivationRadius = 5f;
    
    [Header("UI References")]
    public DirectionIndicator directionIndicator;

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
        // 確保有檢查點
        if (checkpoints.Count == 0)
        {
            GameObject[] cpObjects = GameObject.FindGameObjectsWithTag("Checkpoint");
            foreach (GameObject cp in cpObjects)
            {
                checkpoints.Add(cp.transform);
            }
            
            // 按名稱排序確保順序正確
            checkpoints.Sort((a, b) => a.name.CompareTo(b.name));
        }
    }

    void Update()
    {
        // 檢查玩家是否到達當前檢查點
        if (currentCheckpointIndex < checkpoints.Count)
        {
            Transform currentCP = checkpoints[currentCheckpointIndex];
            float distance = Vector3.Distance(SwimmingController.Instance.transform.position, currentCP.position);
            
            if (distance < checkpointActivationRadius)
            {
                ActivateNextCheckpoint();
            }
        }
    }

    public void ActivateNextCheckpoint()
    {
        currentCheckpointIndex++;
        
        if (currentCheckpointIndex >= checkpoints.Count)
        {
            // 所有檢查點完成，觸發勝利
            LevelManager.Instance.LevelComplete(true);
        }
    }

    public Transform GetCurrentCheckpoint()
    {
        if (currentCheckpointIndex < checkpoints.Count)
        {
            return checkpoints[currentCheckpointIndex];
        }
        return null; // 所有檢查點已完成
    }
}