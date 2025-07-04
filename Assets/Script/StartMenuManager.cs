/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class StartMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectionPanel;
    public GameObject settingsPanel;
    public GameObject difficultyPanel;
    public GameObject levelIntroPanel;

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

    [Header("Level Intro Panel")]
    public Text introTitleText;
    public Text characterLabel;
    public Text characterText;
    public Text mapLabel;
    public Text mapText;
    public Text enemyLabel;
    public Text enemyText;
    public Text introLabel;
    public Text introText;
    public Text conditionLabel;
    public Text conditionText;
    public Button startLevelButton;
    public Button cancelButton;

    [Header("Achievement Icon")]
    public Image achievementIcon;
    public Sprite iconA;
    public Sprite iconB;

    private WaterEffectController waterEffect;
    private QuestionnaireManager questionnaireManager;
    private string selectedLevel;

    [System.Serializable]
    public class LevelIntroContent
    {
        public string character_CN;
        public string character_EN;
        public string map_CN;
        public string map_EN;
        public string enemy_CN;
        public string enemy_EN;
        public string intro_CN;
        public string intro_EN;
        public string condition_CN;
        public string condition_EN;
    }

    void Start()
    {
        ShowMainMenu();

        startButton.onClick.AddListener(StartGame);
        settingsButton.onClick.AddListener(ShowSettings);
        exitButton.onClick.AddListener(ExitGame);
        level1Button.onClick.AddListener(() => ShowLevelIntro("Level1"));
        level2Button.onClick.AddListener(() => ShowLevelIntro("Level2"));
        level3Button.onClick.AddListener(() => ShowLevelIntro("Level3"));
        englishButton.onClick.AddListener(() => SetLanguage(false));
        chineseButton.onClick.AddListener(() => SetLanguage(true));
        backButton.onClick.AddListener(ShowMainMenu);
        deleteSaveButton.onClick.AddListener(DeleteSaveData);
        startLevelButton.onClick.AddListener(LoadSelectedLevel);
        cancelButton.onClick.AddListener(CancelLevelIntro);

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
        achievementIcon.sprite = level3Completed ? iconB : iconA;

        UpdateDifficultyLabels();
        waterEffect = Camera.main.GetComponent<WaterEffectController>();
        if (waterEffect != null)
        {
            waterEffect.UpdateFromPlayerPrefs();
        }

        AddButtonSounds(startButton);
        AddButtonSounds(settingsButton);
        AddButtonSounds(exitButton);
        AddButtonSounds(level1Button);
        AddButtonSounds(level2Button);
        AddButtonSounds(level3Button);
        AddButtonSounds(englishButton);
        AddButtonSounds(chineseButton);
        AddButtonSounds(backButton);
        AddButtonSounds(deleteSaveButton);
        AddButtonSounds(startLevelButton);
        AddButtonSounds(cancelButton);
    }

    private void AddButtonSounds(Button button)
    {
        if (button != null)
        {
            var trigger = button.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((data) => SoundManager.Instance.PlayButtonHover());
            trigger.triggers.Add(entry);
            button.onClick.AddListener(() => SoundManager.Instance.PlayButtonClick());
        }
    }

    public void OnSettingsOpened()
    {
        if (waterEffect != null)
        {
            waterEffect.UpdateFromPlayerPrefs();
        }
    }

    public LevelIntroContent level1Content = new LevelIntroContent
    {
        character_CN = "小丑鱼尼莫",
        character_EN = "Clownfish Nemo",
        map_CN = "珊瑚礁浅海区",
        map_EN = "Shallow Coral Reef",
        enemy_CN = "塑料瓶、渔网、塑料袋",
        enemy_EN = "Plastic bottles, Fishing nets, Plastic bags",
        intro_CN = "作为刚离开家的小丑鱼，你需要穿越充满塑料垃圾的珊瑚礁，寻找失散的家人。注意躲避渔网和有毒污染物。",
        intro_EN = "As a young clownfish who just left home, you must navigate through plastic-polluted coral reefs to find your lost family. Beware of fishing nets and toxic pollutants.",
        condition_CN = "收集10个海藻能量，在氧气耗尽前找到家人",
        condition_EN = "Collect 10 seaweed energies, find family before oxygen runs out"
    };

    public LevelIntroContent level2Content = new LevelIntroContent
    {
        character_CN = "海龟老船长",
        character_EN = "Sea Turtle Captain",
        map_CN = "深海峡谷与沉船区",
        map_EN = "Deep Sea Canyon & Shipwreck Zone",
        enemy_CN = "废弃渔具、漏油、化学污染物",
        enemy_EN = "Abandoned fishing gear, Oil spills, Chemical pollutants",
        intro_CN = "经验丰富的海龟船长需要带领鱼群穿越危险的工业污染区。利用你的硬壳抵御化学污染物，拯救被困的海洋生物。",
        intro_EN = "The experienced sea turtle captain must lead the fish school through dangerous industrial pollution zones. Use your hard shell to resist chemical pollutants and save trapped marine life.",
        condition_CN = "拯救5只被困海洋生物，避开所有油污区",
        condition_EN = "Rescue 5 trapped marine creatures, avoid all oil spill zones"
    };

    public LevelIntroContent level3Content = new LevelIntroContent
    {
        character_CN = "大白鲨守护者",
        character_EN = "Great White Guardian",
        map_CN = "海底火山与工业区",
        map_EN = "Undersea Volcano & Industrial Zone",
        enemy_CN = "工业废水、热污染、声纳干扰",
        enemy_EN = "Industrial wastewater, Thermal pollution, Sonar interference",
        intro_CN = "作为海洋守护者，你需要关闭污染源头的工业管道。利用火山热流加速，但要小心过热区域。最终挑战污染工厂的核心！",
        intro_EN = "As the ocean guardian, you must shut down industrial pipes at the pollution source. Use volcanic heat currents for speed boosts but beware of overheating zones. Final challenge at the core of the pollution factory!",
        condition_CN = "关闭3个污染管道，击败最终BOSS",
        condition_EN = "Shut down 3 pollution pipes, defeat final BOSS"
    };

    public void ShowLevelIntro(string levelName)
    {
        selectedLevel = levelName;
        HideAllPanels();
        levelIntroPanel.SetActive(true);
        UpdateLevelIntroDisplay(levelName);
    }

    private void UpdateLevelIntroDisplay(string levelName)
    {
        LevelIntroContent content = null;
        switch (levelName)
        {
            case "Level1":
                content = level1Content;
                break;
            case "Level2":
                content = level2Content;
                break;
            case "Level3":
                content = level3Content;
                break;
        }
        if (content == null)
        {
            Debug.LogError("未找到关卡内容: " + levelName);
            return;
        }

        if (QuestionnaireManager.isChinese)
        {
            introTitleText.text = "关卡介绍 - " + levelName.Replace("Level", "第") + "关";
            characterLabel.text = "扮演角色:";
            mapLabel.text = "地图环境:";
            enemyLabel.text = "主要威胁:";
            introLabel.text = "任务目标:";
            conditionLabel.text = "通关条件:";
            startLevelButton.GetComponentInChildren<Text>().text = "开始挑战";
            cancelButton.GetComponentInChildren<Text>().text = "返回选择";
            characterText.text = content.character_CN;
            mapText.text = content.map_CN;
            enemyText.text = content.enemy_CN;
            introText.text = content.intro_CN;
            conditionText.text = content.condition_CN;
        }
        else
        {
            introTitleText.text = "Level Intro - " + levelName;
            characterLabel.text = "Play As:";
            mapLabel.text = "Environment:";
            enemyLabel.text = "Main Threats:";
            introLabel.text = "Mission:";
            conditionLabel.text = "Win Condition:";
            startLevelButton.GetComponentInChildren<Text>().text = "Start Level";
            cancelButton.GetComponentInChildren<Text>().text = "Back";
            characterText.text = content.character_EN;
            mapText.text = content.map_EN;
            enemyText.text = content.enemy_EN;
            introText.text = content.intro_EN;
            conditionText.text = content.condition_EN;
        }

        Color bgColor = Color.cyan;
        switch (levelName)
        {
            case "Level1":
                bgColor = new Color(0.2f, 0.8f, 1f, 0.9f);
                break;
            case "Level2":
                bgColor = new Color(0.1f, 0.4f, 0.6f, 0.9f);
                break;
            case "Level3":
                bgColor = new Color(0.8f, 0.3f, 0.2f, 0.9f);
                break;
        }
        levelIntroPanel.GetComponent<Image>().color = bgColor;
    }

    private void LoadSelectedLevel()
    {
        SceneManager.LoadScene(selectedLevel);
    }

    private void CancelLevelIntro()
    {
        levelIntroPanel.SetActive(false);
        ShowLevelSelection();
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
        lockText.text = QuestionnaireManager.isChinese ? "通关第三关后解锁" : "Complete Level 3 to unlock";
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
        levelIntroPanel.SetActive(false);
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

    private void UpdateLanguageUI()
    {
        bool isChinese = QuestionnaireManager.isChinese;
        if (englishButtonHighlight != null)
            englishButtonHighlight.enabled = !isChinese;
        if (chineseButtonHighlight != null)
            chineseButtonHighlight.enabled = isChinese;
        Text deleteButtonText = deleteSaveButton.GetComponentInChildren<Text>();
        if (deleteButtonText != null)
        {
            deleteButtonText.text = isChinese ? "删除存档" : "Delete Save";
        }
        UpdateLevelStatusDisplay();
        UpdateDifficultyLabels();
        if (levelIntroPanel.activeSelf)
        {
            UpdateLevelIntroDisplay(selectedLevel);
        }
        LevelManager[] levelManagers = FindObjectsOfType<LevelManager>();
        foreach (var manager in levelManagers)
        {
            if (manager.tutorialPanelController != null)
            {
                manager.tutorialPanelController.UpdateTutorialText();
            }
        }
    }

    public void SetLanguage(bool useChinese)
    {
        QuestionnaireManager.isChinese = useChinese;
        PlayerPrefs.SetInt("IsChinese", useChinese ? 1 : 0);
        PlayerPrefs.Save();
        UpdateLanguageUI();
        LevelManager[] levelManagers = FindObjectsOfType<LevelManager>();
        foreach (var manager in levelManagers)
        {
            if (manager.tutorialPanelController != null)
            {
                manager.tutorialPanelController.UpdateTutorialText();
            }
        }
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
        achievementIcon.sprite = level3Completed ? iconB : iconA;
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