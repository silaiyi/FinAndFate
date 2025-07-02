/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class QuestionData
{
    public string questionId; // 新增：唯一问题标识符
    public PollutionType pollutionType;
    
    // 英文问题文本
    public string questionText;
    
    // 中文问题文本
    public string questionTextCN;
    
    // 选项数组（包含中英文文本）
    public AnswerOption[] options;
}

[Serializable]
public class AnswerOption
{
    // 英文选项文本
    public string optionText;
    
    // 中文选项文本
    public string optionTextCN;
    
    public int scoreValue;
}

public enum PollutionType
{
    Carbon,
    Trash,
    Fishing,
    Sewage
}