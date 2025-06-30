using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic; // 添加这个命名空间

public class StartMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectionPanel;
    public GameObject settingsPanel;

    [Header("Main Menu Buttons")]
    public Button startButton;

    [Header("Level Selection")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Text level1StatusText;
    public Text level1BestTimeText;

    [Header("Language Buttons")]
    public Button englishButton;
    public Button chineseButton;
    public Image englishButtonHighlight;
    public Image chineseButtonHighlight;

    [Header("Navigation Buttons")]
    public Button backButton;
    public Button deleteSaveButton;

    [Header("Questionnaire Reference")]
    public QuestionnaireManager questionnaireManager;

    void Start()
    {
        // 初始化UI状态
        ShowMainMenu();
        
        // 设置按钮事件监听
        startButton.onClick.AddListener(StartGame);
        level1Button.onClick.AddListener(() => LoadLevel("Level1"));
        level2Button.onClick.AddListener(() => LoadLevel("Level2"));
        level3Button.onClick.AddListener(() => LoadLevel("Level3"));
        englishButton.onClick.AddListener(() => SetLanguage(false));
        chineseButton.onClick.AddListener(() => SetLanguage(true));
        backButton.onClick.AddListener(ShowMainMenu);
        deleteSaveButton.onClick.AddListener(DeleteSaveData);

        // 更新UI显示
        deleteSaveButton.GetComponentInChildren<Text>().text = "Delete Save Data";
        bool isChinese = PlayerPrefs.GetInt("IsChinese", 0) == 1;
        QuestionnaireManager.isChinese = isChinese;
        UpdateLanguageUI();
        UpdateLevelStatusDisplay();
    }

    // =====================
    // UI面板控制方法
    // =====================
    public void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        levelSelectionPanel.SetActive(false);
        settingsPanel.SetActive(false);
        backButton.gameObject.SetActive(false);
        deleteSaveButton.gameObject.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void ShowLevelSelection()
    {
        HideAllPanels();
        levelSelectionPanel.SetActive(true);
        backButton.gameObject.SetActive(true);
        deleteSaveButton.gameObject.SetActive(true);
        UpdateLevelStatusDisplay(); // 每次显示时刷新状态
    }

    public void ShowSettings()
    {
        HideAllPanels();
        settingsPanel.SetActive(true);
        backButton.gameObject.SetActive(true);
        deleteSaveButton.gameObject.SetActive(true);
    }

    // =====================
    // 游戏流程控制方法
    // =====================
    public void StartGame()
    {
        HideAllPanels();

        if (questionnaireManager == null)
        {
            questionnaireManager = FindObjectOfType<QuestionnaireManager>();
        }

        if (questionnaireManager != null)
        {
            questionnaireManager.CheckAndShowQuestionnaire();
        }
        else
        {
            Debug.LogError("QuestionnaireManager not found! Showing level selection directly.");
            ShowLevelSelection();
        }
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // =====================
    // 语言设置方法
    // =====================
    private void UpdateLanguageUI()
    {
        bool isChinese = QuestionnaireManager.isChinese;
        
        if (englishButtonHighlight != null)
            englishButtonHighlight.enabled = !isChinese;
        if (chineseButtonHighlight != null)
            chineseButtonHighlight.enabled = isChinese;
    }

    public void SetLanguage(bool useChinese)
    {
        QuestionnaireManager.isChinese = useChinese;
        PlayerPrefs.SetInt("IsChinese", useChinese ? 1 : 0);
        PlayerPrefs.Save();
        UpdateLanguageUI();
        Debug.Log("Language set to: " + (useChinese ? "Chinese" : "English"));
    }

    // =====================
    // 数据管理方法
    // =====================
    public void DeleteSaveData()
    {
        PlayerPrefs.DeleteKey("QuestionnaireCompleted");
        PlayerPrefs.DeleteKey("CarbonScore");
        PlayerPrefs.DeleteKey("TrashScore");
        PlayerPrefs.DeleteKey("FishingScore");
        PlayerPrefs.DeleteKey("SewageScore");
        
        // 删除关卡完成标记
        PlayerPrefs.DeleteKey("Level1Completed");
        PlayerPrefs.DeleteKey("Level2Completed");
        PlayerPrefs.DeleteKey("Level1BestTime");
        
        // 删除玩家回答记录
        PlayerPrefs.DeleteKey("PlayerAnswerRecords");
        
        PlayerPrefs.Save();
        Debug.Log("Save data deleted! Questionnaire will be shown again.");
        
        // 删除后刷新关卡显示状态
        UpdateLevelStatusDisplay();
    }

    void UpdateLevelStatusDisplay()
    {
        // 获取关卡完成状态
        bool level1Completed = PlayerPrefs.GetInt("Level1Completed", 0) == 1;
        bool level2Completed = PlayerPrefs.GetInt("Level2Completed", 0) == 1;

        // 更新关卡1状态文本
        if (level1StatusText != null)
        {
            level1StatusText.text = level1Completed ? "已完成" : "未完成";
        }

        // 更新关卡1最佳时间文本
        if (level1BestTimeText != null)
        {
            if (level1Completed)
            {
                float bestTime = PlayerPrefs.GetFloat("Level1BestTime", 0);
                int minutes = Mathf.FloorToInt(bestTime / 60f);
                int seconds = Mathf.FloorToInt(bestTime % 60f);
                level1BestTimeText.text = $"最佳: {minutes:00}:{seconds:00}";
            }
            else
            {
                level1BestTimeText.text = "";
            }
        }

        // 设置关卡按钮状态
        level1Button.interactable = true;
        level2Button.interactable = level1Completed;
        level3Button.interactable = level2Completed;
    }

    // =====================
    // 回答记录访问方法
    // =====================
    public static List<QuestionnaireManager.PlayerAnswerRecord> GetPlayerAnswerRecords()
    {
        if (PlayerPrefs.HasKey("PlayerAnswerRecords"))
        {
            string json = PlayerPrefs.GetString("PlayerAnswerRecords");
            AnswerRecordWrapper wrapper = JsonUtility.FromJson<AnswerRecordWrapper>(json);
            return wrapper.records;
        }
        return new List<QuestionnaireManager.PlayerAnswerRecord>();
    }
    
    // JSON反序列化包装类
    [System.Serializable]
    private class AnswerRecordWrapper
    {
        public List<QuestionnaireManager.PlayerAnswerRecord> records;
    }

    public static void ApplyPollutionScores()
    {
        if (PlayerPrefs.HasKey("CarbonScore") && SwimmingController.Instance != null)
        {
            SwimmingController.Instance.carbonScore = PlayerPrefs.GetInt("CarbonScore");
            SwimmingController.Instance.trashScore = PlayerPrefs.GetInt("TrashScore");
            SwimmingController.Instance.fishingScore = PlayerPrefs.GetInt("FishingScore");
            SwimmingController.Instance.sewageScore = PlayerPrefs.GetInt("SewageScore");

            SwimmingController.PollutionScores scores = new SwimmingController.PollutionScores
            {
                carbon = PlayerPrefs.GetInt("CarbonScore"),
                trash = PlayerPrefs.GetInt("TrashScore"),
                fishing = PlayerPrefs.GetInt("FishingScore"),
                sewage = PlayerPrefs.GetInt("SewageScore")
            };

            SwimmingController.Instance.UpdatePollutionScores(scores);
        }
    }
}