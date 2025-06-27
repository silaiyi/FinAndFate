using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject resultPanel;
    public Text resultText;
    public Text timeText;
    public Button replayButton;
    public Button menuButton;

    [Header("Level Settings")]
    public GameObject companionPrefab; // 同伴预制体
    public Transform companionSpawnPoint; // 同伴生成点
    public float companionTriggerRadius = 5f; // 触发半径
    
    private float levelStartTime;
    private bool levelCompleted;
    // 在LevelManager.cs中添加
public GameObject companionInstance { get; private set; } // 修改访问权限为public
    private GameObject player;
    [Header("Localization")]
    public string successTextEN = "Congratulations! You found your companion!";
    public string successTextCN = "恭喜！找到了同伴！";
    public string failTextEN = "Game Over, Try Again";
    public string failTextCN = "遊戲失敗，請再接再厲";
    public string replayTextEN = "Replay";
    public string replayTextCN = "重新挑戰";
    public string menuTextEN = "Main Menu";
    public string menuTextCN = "主選單";

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
        player = GameObject.FindGameObjectWithTag("Player");
        SpawnCompanion();

        levelStartTime = Time.time;
        levelCompleted = false;

        // 初始化UI
        resultPanel.SetActive(false);
        replayButton.onClick.AddListener(RestartLevel);
        menuButton.onClick.AddListener(ReturnToMenu);
        if (replayButton != null)
        {
            Text replayBtnText = replayButton.GetComponentInChildren<Text>();
            if (replayBtnText != null)
                replayBtnText.text = QuestionnaireManager.isChinese ? replayTextCN : replayTextEN;
        }
        
        if (menuButton != null)
        {
            Text menuBtnText = menuButton.GetComponentInChildren<Text>();
            if (menuBtnText != null)
                menuBtnText.text = QuestionnaireManager.isChinese ? menuTextCN : menuTextEN;
        }
    }

    void Update()
    {
        // 检查玩家是否到达同伴位置
        if (!levelCompleted && companionInstance != null && 
            Vector3.Distance(player.transform.position, companionInstance.transform.position) < companionTriggerRadius)
        {
            LevelComplete(true);
        }
    }

    void SpawnCompanion()
    {
        if (companionPrefab != null && companionSpawnPoint != null)
        {
            companionInstance = Instantiate(companionPrefab, companionSpawnPoint.position, companionSpawnPoint.rotation);
            companionInstance.tag = "Companion";
            
            // 添加高亮效果
            if (OutlineManager.Instance != null)
            {
                OutlineManager.Instance.ApplyOutline(companionInstance, Color.green);
            }
        }
    }

    public void LevelComplete(bool success)
    {
        if (levelCompleted) return;
        
        levelCompleted = true;
        float elapsedTime = Time.time - levelStartTime;
        
        // 更新UI
        resultText.text = QuestionnaireManager.isChinese ? 
            (success ? successTextCN : failTextCN) :
            (success ? successTextEN : failTextEN);
        timeText.text = FormatTime(elapsedTime);
        resultPanel.SetActive(true);
        
        // 暂停游戏
        Time.timeScale = 0f;
        
        // 保存通关数据
        if (success)
        {
            SaveLevelCompletion();
        }
    }

    string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    void SaveLevelCompletion()
    {
        PlayerPrefs.SetInt("Level1Completed", 1);
        
        // 保存最佳时间（如果比之前的好）
        float bestTime = PlayerPrefs.GetFloat("Level1BestTime", float.MaxValue);
        float currentTime = Time.time - levelStartTime;
        
        if (currentTime < bestTime)
        {
            PlayerPrefs.SetFloat("Level1BestTime", currentTime);
        }
        
        PlayerPrefs.Save();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        
        // 销毁 SwimmingController 实例
        if (SwimmingController.Instance != null)
        {
            Destroy(SwimmingController.Instance.gameObject);
        }
        
        // 重新加载场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        
        if (SwimmingController.Instance != null)
        {
            Destroy(SwimmingController.Instance.gameObject);
        }
        
        SceneManager.LoadScene("StartMenu");
    }
}