using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class QuestionData
{
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