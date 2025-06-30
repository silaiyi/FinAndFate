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
    private float _levelStartTime;
    private bool _levelCompleted;
    private float _currentElapsedTime; // 重命名變量避免衝突
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
    private SafeZoneController safeZone; // 新增声明
    // 添加事件委托
    public delegate void LevelCompleteEvent(bool success);
    public static event LevelCompleteEvent OnLevelComplete;

    void Awake()
    {
        // 修复单例初始化问题
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        
        // 确保所有必要组件初始化
        if (replayButton != null) replayButton.onClick.AddListener(RestartLevel);
        if (menuButton != null) menuButton.onClick.AddListener(ReturnToMenu);
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        SpawnCompanion();
        ResetLevelState(); // 重置关卡状态
        if (SceneManager.GetActiveScene().name == "Level2")
        {
            safeZone = FindObjectOfType<SafeZoneController>();
            if (safeZone != null)
            {
                Debug.Log("安全区初始化完成");
            }
        }

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
        _levelStartTime = Time.time;
        _levelCompleted = false;
        SpawnCompanion();
        ResetLevelState();
    }

    // LevelManager.cs - 修改 Update 方法中的安全区缩小逻辑
    void Update()
    {
        // 計算當前經過時間
        _currentElapsedTime = Time.time - _levelStartTime;
        
        // 檢查玩家是否到達同伴位置
        if (!_levelCompleted && companionInstance != null && 
            Vector3.Distance(player.transform.position, companionInstance.transform.position) < companionTriggerRadius)
        {
            LevelComplete(true);
        }
        
        // 第二關特殊勝利條件：存活5分鐘
        if (!_levelCompleted && SceneManager.GetActiveScene().name == "Level2")
        {
            // 檢查是否存活5分鐘（300秒）
            if (_currentElapsedTime >= 300f)
            {
                LevelComplete(true);
            }
            
            // 安全区缩小逻辑 - 只启动一次
            if (!_safeZoneStarted && _currentElapsedTime >= 0f) // 游戏开始后立即启动
            {
                SafeZoneController safeZone = FindObjectOfType<SafeZoneController>();
                if (safeZone != null)
                {
                    safeZone.StartShrinking();
                    _safeZoneStarted = true; // 标记已启动
                }
            }
        }
    }

    // 在类中添加私有变量
    private bool _safeZoneStarted = false;

    void SpawnCompanion()
    {
        if (companionPrefab != null && companionSpawnPoint != null)
        {
            companionInstance = Instantiate(companionPrefab, companionSpawnPoint.position, companionSpawnPoint.rotation);
            companionInstance.tag = "Companion";
            
        }
    }

    public void LevelComplete(bool success)
    {
        if (_levelCompleted) return;
        
        _levelCompleted = true;
        
        // 根據關卡類型顯示不同的勝利信息
        string victoryMessage = "";
        if (SceneManager.GetActiveScene().name == "Level2" && success)
        {
            victoryMessage = QuestionnaireManager.isChinese ? 
                "恭喜！成功存活！" : 
                "Congratulations!";
        }
        else
        {
            victoryMessage = QuestionnaireManager.isChinese ? 
                (success ? successTextCN : failTextCN) :
                (success ? successTextEN : failTextEN);
        }
        
        resultText.text = victoryMessage;
        timeText.text = FormatTime(_currentElapsedTime);
        resultPanel.SetActive(true);
        
        // 暫停遊戲
        Time.timeScale = 0f;
        if (OnLevelComplete != null)
        {
            OnLevelComplete(success);
        }
        
        // 保存通關數據
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
        string currentLevel = SceneManager.GetActiveScene().name;
        
        if (currentLevel == "Level1")
        {
            PlayerPrefs.SetInt("Level1Completed", 1);
            
            // 保存最佳时间（如果比之前的好）
            float bestTime = PlayerPrefs.GetFloat("Level1BestTime", float.MaxValue);
            float currentTime = Time.time - levelStartTime;
            
            if (currentTime < bestTime)
            {
                PlayerPrefs.SetFloat("Level1BestTime", currentTime);
            }
        }
        else if (currentLevel == "Level2")
        {
            // 新增第二关完成标记
            PlayerPrefs.SetInt("Level2Completed", 1);
        }
        
        PlayerPrefs.Save();
    }

    // LevelManager.cs
    public void RestartLevel()
    {
        Time.timeScale = 1f;

        // 重置玩家状态
        if (SwimmingController.Instance != null)
        {
            SwimmingController.Instance.ResetPlayerState();
        }

        // 重置安全区（如果是Level2）
        if (SceneManager.GetActiveScene().name == "Level2" && safeZone != null)
        {
            safeZone.ResetSafeZone();
        }

        // 重新加载场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("RestartLevel called");
    }

    // 在 LevelManager 类中添加
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重新加载场景时重置状态
        if (scene.name == SceneManager.GetActiveScene().name)
        {
            Debug.Log("Scene reloaded, resetting level state");
            ResetLevelState();
            
            // 重置安全区状态
            _safeZoneStarted = false;
            
            // 重新获取玩家引用
            player = GameObject.FindGameObjectWithTag("Player");
            
            // 重新获取安全区控制器（如果是Level2）
            if (scene.name == "Level2")
            {
                safeZone = FindObjectOfType<SafeZoneController>();
                if (safeZone != null)
                {
                    Debug.Log("SafeZoneController reinitialized");
                }
            }
        }
    }

    // 修改 ResetLevelState 方法
    public void ResetLevelState()
    {
        levelCompleted = false;
        levelStartTime = Time.time;
        _levelStartTime = Time.time;
        _levelCompleted = false;
        
        // 确保同伴被销毁
        if (companionInstance != null)
        {
            Destroy(companionInstance);
            companionInstance = null;
        }
        
        // 重新生成同伴
        SpawnCompanion();
        
        // 隐藏结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        // 重置安全区状态
        _safeZoneStarted = false;
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