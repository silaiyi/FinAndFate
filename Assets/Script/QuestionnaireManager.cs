using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class QuestionnaireManager : MonoBehaviour
{
    [Header("UI References")]
    public Text questionText;
    public Button[] optionButtons;
    public Text progressText;
    public GameObject questionnairePanel;

    [Header("Question Data")]
    public List<QuestionData> allQuestions = new List<QuestionData>();

    private List<QuestionData> currentQuestions = new List<QuestionData>();
    private int currentQuestionIndex = 0;
    private int[] categoryScores = new int[4];
    
    // 使用PlayerPrefs持久化存储语言设置
    public static bool isChinese {
        get { return PlayerPrefs.GetInt("IsChinese", 0) == 1; }
        set { PlayerPrefs.SetInt("IsChinese", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    void Start()
    {
        questionnairePanel.SetActive(false);
    }

    private bool ShouldShowQuestionnaire()
    {
        return PlayerPrefs.GetInt("QuestionnaireCompleted", 0) == 0;
    }

    private void InitializeQuestionnaire()
    {
        currentQuestionIndex = 0;
        categoryScores = new int[4];
        
        var groupedQuestions = allQuestions
            .GroupBy(q => q.pollutionType)
            .ToDictionary(g => g.Key, g => g.ToList());

        currentQuestions.Clear();
        foreach (var category in groupedQuestions)
        {
            for (int i = 0; i < 2; i++)
            {
                if (category.Value.Count > 0)
                {
                    int randomIndex = Random.Range(0, category.Value.Count);
                    currentQuestions.Add(category.Value[randomIndex]);
                    category.Value.RemoveAt(randomIndex);
                }
            }
        }

        currentQuestions = currentQuestions.OrderBy(q => Random.value).ToList();
    }

    public void ShowQuestionnaire()
    {
        // 确保隐藏所有其他UI
        FindObjectOfType<StartMenuManager>()?.HideAllPanels();
        
        questionnairePanel.SetActive(true);
        DisplayCurrentQuestion();
    }
    
    public static void ToggleLanguage()
    {
        isChinese = !isChinese;
        Debug.Log("Language set to: " + (isChinese ? "Chinese" : "English"));
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= currentQuestions.Count)
        {
            CompleteQuestionnaire();
            return;
        }

        QuestionData currentQuestion = currentQuestions[currentQuestionIndex];
        questionText.text = isChinese ? currentQuestion.questionTextCN : currentQuestion.questionText;
        progressText.text = (isChinese ? "問題 " : "Question ") + $"{currentQuestionIndex + 1}/{currentQuestions.Count}";

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentQuestion.options.Length)
            {
                int optionIndex = i;
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<Text>().text = 
                    isChinese ? currentQuestion.options[i].optionTextCN : currentQuestion.options[i].optionText;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SelectAnswer(optionIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectAnswer(int optionIndex)
    {
        QuestionData currentQuestion = currentQuestions[currentQuestionIndex];

        if (optionIndex < currentQuestion.options.Length)
        {
            int score = currentQuestion.options[optionIndex].scoreValue;

            switch (currentQuestion.pollutionType)
            {
                case PollutionType.Carbon: categoryScores[0] += score; break;
                case PollutionType.Trash: categoryScores[1] += score; break;
                case PollutionType.Fishing: categoryScores[2] += score; break;
                case PollutionType.Sewage: categoryScores[3] += score; break;
            }
        }

        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    private void CompleteQuestionnaire()
    {
        PlayerPrefs.SetInt("QuestionnaireCompleted", 1);
        PlayerPrefs.SetInt("CarbonScore", categoryScores[0]);
        PlayerPrefs.SetInt("TrashScore", categoryScores[1]);
        PlayerPrefs.SetInt("FishingScore", categoryScores[2]);
        PlayerPrefs.SetInt("SewageScore", categoryScores[3]);
        PlayerPrefs.Save();
        
        questionnairePanel.SetActive(false);
        
        // 显示选关界面
        FindObjectOfType<StartMenuManager>()?.ShowLevelSelection();
    }

    public void CheckAndShowQuestionnaire()
    {
        if (ShouldShowQuestionnaire())
        {
            InitializeQuestionnaire();
            ShowQuestionnaire();
        }
        else
        {
            FindObjectOfType<StartMenuManager>()?.ShowLevelSelection();
        }
    }
}