using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class StartMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectionPanel;
    public GameObject settingsPanel;
    public GameObject difficultyPanel;

    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button settingsButton;
    public Button exitButton;

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

    [Header("Difficulty Settings")]
    public Slider carbonSlider;
    public Slider trashSlider;
    public Slider fishingSlider;
    public Slider sewageSlider;
    public Text carbonLabel;
    public Text trashLabel;
    public Text fishingLabel;
    public Text sewageLabel;
    public GameObject difficultyLockPanel;
    public Text lockText;
    public Text difficultyTitleText;

    private QuestionnaireManager questionnaireManager;

    void Start()
    {
        ShowMainMenu();
        
        startButton.onClick.AddListener(StartGame);
        settingsButton.onClick.AddListener(ShowSettings);
        exitButton.onClick.AddListener(ExitGame);
        level1Button.onClick.AddListener(() => LoadLevel("Level1"));
        level2Button.onClick.AddListener(() => LoadLevel("Level2"));
        level3Button.onClick.AddListener(() => LoadLevel("Level3"));
        englishButton.onClick.AddListener(() => SetLanguage(false));
        chineseButton.onClick.AddListener(() => SetLanguage(true));
        backButton.onClick.AddListener(ShowMainMenu);
        deleteSaveButton.onClick.AddListener(DeleteSaveData);

        bool isChinese = PlayerPrefs.GetInt("IsChinese", 0) == 1;
        QuestionnaireManager.isChinese = isChinese;
        UpdateLanguageUI();
        UpdateLevelStatusDisplay();

        carbonSlider.onValueChanged.AddListener(OnCarbonSliderChanged);
        trashSlider.onValueChanged.AddListener(OnTrashSliderChanged);
        fishingSlider.onValueChanged.AddListener(OnFishingSliderChanged);
        sewageSlider.onValueChanged.AddListener(OnSewageSliderChanged);

        carbonSlider.value = PlayerPrefs.GetInt("CarbonScore", 0);
        trashSlider.value = PlayerPrefs.GetInt("TrashScore", 0);
        fishingSlider.value = PlayerPrefs.GetInt("FishingScore", 0);
        sewageSlider.value = PlayerPrefs.GetInt("SewageScore", 0);

        bool level3Completed = PlayerPrefs.GetInt("Level3Completed", 0) == 1;
        difficultyLockPanel.SetActive(!level3Completed);

        UpdateDifficultyLabels();
    }

    private void OnCarbonSliderChanged(float value)
    {
        PlayerPrefs.SetInt("CarbonScore", (int)value);
        PlayerPrefs.Save();
        UpdateDifficultyLabels();
    }

    private void OnTrashSliderChanged(float value)
    {
        PlayerPrefs.SetInt("TrashScore", (int)value);
        PlayerPrefs.Save();
        UpdateDifficultyLabels();
    }

    private void OnFishingSliderChanged(float value)
    {
        PlayerPrefs.SetInt("FishingScore", (int)value);
        PlayerPrefs.Save();
        UpdateDifficultyLabels();
    }

    private void OnSewageSliderChanged(float value)
    {
        PlayerPrefs.SetInt("SewageScore", (int)value);
        PlayerPrefs.Save();
        UpdateDifficultyLabels();
    }

    public void UpdateDifficultyLabels()
    {
        carbonLabel.text = (QuestionnaireManager.isChinese ? "碳排放: " : "Carbon: ") + (int)carbonSlider.value;
        trashLabel.text = (QuestionnaireManager.isChinese ? "垃圾污染: " : "Trash: ") + (int)trashSlider.value;
        fishingLabel.text = (QuestionnaireManager.isChinese ? "过度捕捞: " : "Fishing: ") + (int)fishingSlider.value;
        sewageLabel.text = (QuestionnaireManager.isChinese ? "污水排放: " : "Sewage: ") + (int)sewageSlider.value;
        
        lockText.text = QuestionnaireManager.isChinese ? 
            "通关第三关后解锁" : 
            "Complete Level 3 to unlock";
            
        if (difficultyTitleText != null)
        {
            difficultyTitleText.text = QuestionnaireManager.isChinese ? "难度设置" : "Difficulty Settings";
        }
    }

    public void ShowDifficultySettings()
    {
        HideAllPanels();
        difficultyPanel.SetActive(true);
        backButton.gameObject.SetActive(true);
    }

    public void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        levelSelectionPanel.SetActive(false);
        settingsPanel.SetActive(false);
        difficultyPanel.SetActive(false);
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
        UpdateLevelStatusDisplay();
    }

    public void ShowSettings()
    {
        HideAllPanels();
        settingsPanel.SetActive(true);
        difficultyPanel.SetActive(true);
        backButton.gameObject.SetActive(true);
        deleteSaveButton.gameObject.SetActive(true);
    }

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

    private void UpdateLanguageUI()
    {
        bool isChinese = QuestionnaireManager.isChinese;
        
        if (englishButtonHighlight != null)
            englishButtonHighlight.enabled = !isChinese;
        if (chineseButtonHighlight != null)
            chineseButtonHighlight.enabled = isChinese;
            
        UpdateLevelStatusDisplay();
        UpdateDifficultyLabels();
    }

    public void SetLanguage(bool useChinese)
    {
        QuestionnaireManager.isChinese = useChinese;
        PlayerPrefs.SetInt("IsChinese", useChinese ? 1 : 0);
        PlayerPrefs.Save();
        UpdateLanguageUI();
    }

    public void DeleteSaveData()
    {
        PlayerPrefs.DeleteKey("QuestionnaireCompleted");
        PlayerPrefs.DeleteKey("CarbonScore");
        PlayerPrefs.DeleteKey("TrashScore");
        PlayerPrefs.DeleteKey("FishingScore");
        PlayerPrefs.DeleteKey("SewageScore");
        PlayerPrefs.DeleteKey("Level1Completed");
        PlayerPrefs.DeleteKey("Level2Completed");
        PlayerPrefs.DeleteKey("Level3Completed");
        PlayerPrefs.DeleteKey("Level1BestTime");
        PlayerPrefs.DeleteKey("PlayerAnswerRecords");
        PlayerPrefs.Save();
        
        UpdateLevelStatusDisplay();
        
        bool level3Completed = PlayerPrefs.GetInt("Level3Completed", 0) == 1;
        difficultyLockPanel.SetActive(!level3Completed);
    }

    void UpdateLevelStatusDisplay()
    {
        bool level1Completed = PlayerPrefs.GetInt("Level1Completed", 0) == 1;
        bool level2Completed = PlayerPrefs.GetInt("Level2Completed", 0) == 1;
        bool level3Completed = PlayerPrefs.GetInt("Level3Completed", 0) == 1;

        if (level1StatusText != null)
        {
            level1StatusText.text = level1Completed ? 
                (QuestionnaireManager.isChinese ? "已完成" : "Completed") : 
                (QuestionnaireManager.isChinese ? "未完成" : "Not Completed");
        }

        if (level1BestTimeText != null && level1Completed)
        {
            float bestTime = PlayerPrefs.GetFloat("Level1BestTime", 0);
            int minutes = Mathf.FloorToInt(bestTime / 60f);
            int seconds = Mathf.FloorToInt(bestTime % 60f);
            level1BestTimeText.text = QuestionnaireManager.isChinese ? 
                $"最佳: {minutes:00}:{seconds:00}" : 
                $"Best: {minutes:00}:{seconds:00}";
        }
        else if (level1BestTimeText != null)
        {
            level1BestTimeText.text = "";
        }

        level1Button.interactable = true;
        level2Button.interactable = level1Completed;
        level3Button.interactable = level2Completed;
    }

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