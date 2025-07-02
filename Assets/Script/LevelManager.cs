using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.Collections.Generic;

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
    public GameObject companionPrefab;
    public Transform companionSpawnPoint;
    public float companionTriggerRadius = 5f;
    
    private float levelStartTime;
    private bool levelCompleted;
    private float currentElapsedTime;
    public GameObject companionInstance { get; private set; }
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
    private SafeZoneController safeZone;
    
    public delegate void LevelCompleteEvent(bool success);
    public static event LevelCompleteEvent OnLevelComplete;
    
    [Header("Ending Credits")]
    public GameObject creditsPanel;
    public Text creditsText;
    public float scrollSpeed = 30f;
    public Button skipButton;
    public Text difficultyTitleText;
    public Text creditsTitleText;
    public Text skipText;
    
    public string creditsTitleEN = "Environmental Knowledge Review";
    public string creditsTitleCN = "环保知识问答回顾";
    public string skipButtonEN = "Skip";
    public string skipButtonCN = "跳过";
    
    private Coroutine creditsCoroutine;
    private bool safeZoneStarted = false;

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
        
        if (replayButton != null) replayButton.onClick.AddListener(RestartLevel);
        if (menuButton != null) menuButton.onClick.AddListener(ReturnToMenu);
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        ResetLevelState();

        if (SceneManager.GetActiveScene().name == "Level2")
        {
            safeZone = FindObjectOfType<SafeZoneController>();
        }

        levelStartTime = Time.time;
        levelCompleted = false;

        resultPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        
        replayButton.onClick.AddListener(RestartLevel);
        menuButton.onClick.AddListener(ReturnToMenu);
        
        UpdateButtonTexts();
    }

    void Update()
    {
        currentElapsedTime = Time.time - levelStartTime;
        
        if (!levelCompleted && companionInstance != null && 
            Vector3.Distance(player.transform.position, companionInstance.transform.position) < companionTriggerRadius)
        {
            LevelComplete(true);
        }
        
        if (!levelCompleted && SceneManager.GetActiveScene().name == "Level2")
        {
            if (currentElapsedTime >= 300f)
            {
                LevelComplete(true);
            }
            
            if (!safeZoneStarted && currentElapsedTime >= 0f)
            {
                if (safeZone != null)
                {
                    safeZone.StartShrinking();
                    safeZoneStarted = true;
                }
            }
        }
    }

    void SpawnCompanion()
    {
        if (companionPrefab != null && companionSpawnPoint != null)
        {
            if (companionInstance != null)
            {
                Destroy(companionInstance);
            }
            companionInstance = Instantiate(companionPrefab, companionSpawnPoint.position, companionSpawnPoint.rotation);
            companionInstance.tag = "Companion";
        }
    }

    public void LevelComplete(bool success)
    {
        if (levelCompleted) return;

        levelCompleted = true;

        string victoryMessage = "";
        if (SceneManager.GetActiveScene().name == "Level2" && success)
        {
            victoryMessage = QuestionnaireManager.isChinese ? "恭喜！成功存活！" : "Congratulations!";
        }
        else
        {
            victoryMessage = QuestionnaireManager.isChinese ?
                (success ? successTextCN : failTextCN) :
                (success ? successTextEN : failTextEN);
        }

        resultText.text = victoryMessage;
        timeText.text = FormatTime(currentElapsedTime);
        resultPanel.SetActive(true);

        Time.timeScale = 0f;
        OnLevelComplete?.Invoke(success);

        if (success)
        {
            SaveLevelCompletion();
        }
        
        if (success && SceneManager.GetActiveScene().name == "Level3")
        {
            ShowEndingCredits();
        }
    }

    private void ShowEndingCredits()
    {
        resultPanel.SetActive(false);
        creditsPanel.SetActive(true);

        creditsText.text = GenerateEndingText();
        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(SkipCredits);
        creditsCoroutine = StartCoroutine(ScrollCredits());
        
        UpdateCreditsUI();
    }

    private IEnumerator ScrollCredits()
    {
        RectTransform textRect = creditsText.GetComponent<RectTransform>();
        float startY = -Screen.height;
        float targetY = creditsText.preferredHeight + Screen.height;
        Vector3 startPos = new Vector3(textRect.localPosition.x, startY, textRect.localPosition.z);
        textRect.localPosition = startPos;
        
        while (textRect.localPosition.y < targetY)
        {
            textRect.Translate(Vector3.up * scrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        
        ReturnToMenu();
    }

    private void SkipCredits()
    {
        if (creditsCoroutine != null)
        {
            StopCoroutine(creditsCoroutine);
        }
        ReturnToMenu();
    }

    private string GenerateEndingText()
    {
        StringBuilder sb = new StringBuilder();
        
        List<QuestionData> allQuestions = GetQuestionnaireData();
        List<QuestionnaireManager.PlayerAnswerRecord> playerAnswers = StartMenuManager.GetPlayerAnswerRecords();
        
        sb.AppendLine(QuestionnaireManager.isChinese ? "=== 环保知识问答回顾 ===" : "=== Environmental Knowledge Review ===");
        sb.AppendLine();
        
        foreach (QuestionData question in allQuestions)
        {
            sb.AppendLine(QuestionnaireManager.isChinese ? question.questionTextCN : question.questionText);
            sb.AppendLine();
            
            var playerAnswer = playerAnswers.Find(a => a.questionId == question.questionId);
            if (playerAnswer != null)
            {
                string playerChoice = QuestionnaireManager.isChinese ? 
                    question.options[playerAnswer.selectedOptionIndex].optionTextCN :
                    question.options[playerAnswer.selectedOptionIndex].optionText;
                
                sb.AppendLine((QuestionnaireManager.isChinese ? "您的选择: " : "Your choice: ") + playerChoice);
            }
            
            for (int i = 0; i < question.options.Length; i++)
            {
                if (question.options[i].scoreValue == 0)
                {
                    string bestChoice = QuestionnaireManager.isChinese ? 
                        question.options[i].optionTextCN :
                        question.options[i].optionText;
                    
                    sb.AppendLine((QuestionnaireManager.isChinese ? "最优解: " : "Best solution: ") + bestChoice);
                    break;
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("------------------------");
            sb.AppendLine();
        }
        
        sb.AppendLine(QuestionnaireManager.isChinese ? 
            "感谢您为海洋保护做出的贡献！" : 
            "Thank you for your contribution to ocean conservation!");
        
        return sb.ToString();
    }

    private List<QuestionData> GetQuestionnaireData()
    {
        if (PlayerPrefs.HasKey("AllQuestionsData"))
        {
            string json = PlayerPrefs.GetString("AllQuestionsData");
            return JsonUtility.FromJson<QuestionListWrapper>(json).questions;
        }
        return new List<QuestionData>();
    }

    [System.Serializable]
    private class QuestionListWrapper
    {
        public List<QuestionData> questions;
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
            float bestTime = PlayerPrefs.GetFloat("Level1BestTime", float.MaxValue);
            if (currentElapsedTime < bestTime)
            {
                PlayerPrefs.SetFloat("Level1BestTime", currentElapsedTime);
            }
        }
        else if (currentLevel == "Level2")
        {
            PlayerPrefs.SetInt("Level2Completed", 1);
        }
        else if (currentLevel == "Level3")
        {
            PlayerPrefs.SetInt("Level3Completed", 1);
        }
        
        PlayerPrefs.Save();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        if (SwimmingController.Instance != null)
        {
            SwimmingController.Instance.ResetPlayerState();
        }

        if (SceneManager.GetActiveScene().name == "Level2" && safeZone != null)
        {
            safeZone.ResetSafeZone();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

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
        if (scene.name == SceneManager.GetActiveScene().name)
        {
            ResetLevelState();
            safeZoneStarted = false;
            player = GameObject.FindGameObjectWithTag("Player");
            
            if (scene.name == "Level2")
            {
                safeZone = FindObjectOfType<SafeZoneController>();
            }
        }
    }

    public void ResetLevelState()
    {
        levelCompleted = false;
        levelStartTime = Time.time;
        safeZoneStarted = false;
        
        if (companionInstance != null)
        {
            Destroy(companionInstance);
        }
        
        SpawnCompanion();
        
        if (resultPanel != null) resultPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
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
    
    private void UpdateButtonTexts()
    {
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
    
    private void UpdateCreditsUI()
    {
        if (skipButton != null)
        {
            Text skipBtnText = skipButton.GetComponentInChildren<Text>();
            if (skipBtnText != null)
                skipBtnText.text = QuestionnaireManager.isChinese ? skipButtonCN : skipButtonEN;
        }

        if (creditsTitleText != null)
        {
            creditsTitleText.text = QuestionnaireManager.isChinese ? creditsTitleCN : creditsTitleEN;
        }
    }
}