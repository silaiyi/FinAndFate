/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tutorialPanel;
    public Text tutorialText;

    [Header("Localization")]
    public string tutorialTextEN = "Use W,A,S,D to move. Press Space to boost forward, but consumes HP. Avoid plastic trash to prevent HP loss. Press \"Enter\" to hide.";
    public string tutorialTextCN = "使用W,A,S,D進行上下左右操作,並且透過空格加速向前,但需要消耗HP。需要躲避塑膠垃圾否則會導致HP下降。按\"Enter\"以隱藏";

    private bool isVisible = true;

    void Start()
    {
        UpdateTutorialText();
        tutorialPanel.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ToggleTutorialPanel();
        }
    }

    private void ToggleTutorialPanel()
    {
        isVisible = !isVisible;
        tutorialPanel.SetActive(isVisible);
    }

    public void UpdateTutorialText()
    {
        if (tutorialText != null)
        {
            tutorialText.text = QuestionnaireManager.isChinese ? tutorialTextCN : tutorialTextEN;
        }
    }
}
