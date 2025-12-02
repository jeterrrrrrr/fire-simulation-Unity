using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//public enum QuestionKind
//{
//    MultipleChoice,
//    TrueFalse
//}

[System.Serializable]
public class QuestionRecord
{
    public QuestionKind kind;        // MultipleChoice / TrueFalse
    public string questionText;

    public string optionA;
    public string optionB;
    public string optionC;
    public string optionD;

    public string chosenAnswer;      // 玩家實際選的（A/B/C/D 或 O/X）
    public string correctAnswer;     // 正確答案
    public string explanation;       // 詳解
}

[System.Serializable]
public class QuizResult
{
    public string dateTime;          // "2025-11-25 22:30"
    public int score;                // 總分
    public int totalQuestions;       // 題數
    public List<QuestionRecord> questions = new List<QuestionRecord>();
}

[System.Serializable]
public class QuizResultListWrapper
{
    public List<QuizResult> results = new List<QuizResult>();
}

public static class QuizHistoryStorage
{
    private const string KEY = "QuizHistory";

    public static QuizResultListWrapper LoadAll()
    {
        string json = PlayerPrefs.GetString(KEY, "");
        if (string.IsNullOrEmpty(json))
            return new QuizResultListWrapper();

        return JsonUtility.FromJson<QuizResultListWrapper>(json);
    }

    public static void SaveAll(QuizResultListWrapper wrapper)
    {
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public static void AddResult(QuizResult result)
    {
        var wrapper = LoadAll();
        wrapper.results.Add(result);

        // 依日期由新到舊排序（如果你想由舊到新，就改成 OrderBy）
        wrapper.results = wrapper.results
            .OrderByDescending(r => r.dateTime, StringComparer.Ordinal)
            .ToList();

        SaveAll(wrapper);
    }
}
