using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ChoiceOptions
{
    public string A;
    public string B;
    public string C;
    public string D;
}

[System.Serializable]
public class MultipleChoiceQuestion
{
    public int question_number;
    public string question;
    public ChoiceOptions options;
    public string correct_answer;
    public string explanation;
}

[System.Serializable]
public class TrueFalseQuestion
{
    public int question_number;
    public string question;
    public string correct_answer;
    public string explanation;
}

[System.Serializable]
public class MultipleChoiceRoot
{
    public List<MultipleChoiceQuestion> questions;
}

[System.Serializable]
public class TrueFalseRoot
{
    public List<TrueFalseQuestion> questions;
}

public enum QuestionKind
{
    MultipleChoice,
    TrueFalse
}

public class QuizQuestionRuntime
{
    public QuestionKind kind;
    public int questionNumber;
    public string questionText;

    public string optionA;
    public string optionB;
    public string optionC;
    public string optionD;

    public string correctAnswer;
    public string explanation;
}

public class FireQuizManager : MonoBehaviour
{
    [Header("JSON 資料")]
    public TextAsset multipleChoiceJson;
    public TextAsset trueFalseJson;

    [Header("UI - 文字")]
    public Text txtQuestion;
    public Text txtScore;
    public Text txtExplanation;

    [Header("UI - 選擇題按鈕")]
    public Button btnA;
    public Text txtA;
    public Button btnB;
    public Text txtB;
    public Button btnC;
    public Text txtC;
    public Button btnD;
    public Text txtD;

    [Header("UI - 是非題按鈕")]
    public Button btnO;
    public Text txtO;
    public Button btnX;
    public Text txtX;

    [Header("UI - 下一題按鈕")]
    public Button btnNext;   // ★ 新增：顯示詳解後才出現的「下一題」按鈕

    const int QUESTIONS_PER_TYPE = 5;
    const int SCORE_PER_QUESTION = 10;

    List<QuizQuestionRuntime> quizList = new List<QuizQuestionRuntime>();
    int currentIndex = 0;
    int score = 0;
    bool isAnswered = false;

    void Start()
    {
        InitQuiz();
    }

    void InitQuiz()
    {
        LoadQuestions();
        SetupButtonEvents();
        score = 0;
        currentIndex = 0;
        UpdateScoreUI();
        ShowCurrentQuestion();
    }

    void LoadQuestions()
    {
        if (multipleChoiceJson == null || trueFalseJson == null)
        {
            Debug.LogError("請在 Inspector 指定 multipleChoiceJson / trueFalseJson");
            return;
        }

        // 先把原始文字抓出來
        string mcRaw = multipleChoiceJson.text;
        string tfRaw = trueFalseJson.text;

        // 避免 JSON 裡有單一 '\' 造成轉義錯誤
        string mcFixed = mcRaw.Replace("\\", "\\\\");
        string tfFixed = tfRaw.Replace("\\", "\\\\");

        var mcRoot = JsonUtility.FromJson<MultipleChoiceRoot>(mcFixed);
        var tfRoot = JsonUtility.FromJson<TrueFalseRoot>(tfFixed);

        if (mcRoot == null || mcRoot.questions == null)
        {
            Debug.LogError("無法解析 multiple_choice.json（mcRoot 為 null），請檢查格式。");
            return;
        }
        if (tfRoot == null || tfRoot.questions == null)
        {
            Debug.LogError("無法解析 true_false.json（tfRoot 為 null），請檢查格式。");
            return;
        }

        var mcList = mcRoot.questions.Select(q => new QuizQuestionRuntime
        {
            kind = QuestionKind.MultipleChoice,
            questionNumber = q.question_number,
            questionText = q.question,
            optionA = q.options.A,
            optionB = q.options.B,
            optionC = q.options.C,
            optionD = q.options.D,
            correctAnswer = q.correct_answer.Trim().ToUpper(),
            explanation = q.explanation
        }).ToList();

        var tfList = tfRoot.questions.Select(q => new QuizQuestionRuntime
        {
            kind = QuestionKind.TrueFalse,
            questionNumber = q.question_number,
            questionText = q.question,
            correctAnswer = q.correct_answer.Trim().ToUpper(), // O / X
            explanation = q.explanation
        }).ToList();

        Shuffle(mcList);
        Shuffle(tfList);

        int takeMC = Mathf.Min(QUESTIONS_PER_TYPE, mcList.Count);
        int takeTF = Mathf.Min(QUESTIONS_PER_TYPE, tfList.Count);

        // 先決定要用哪些題
        var selectedTF = tfList.Take(takeTF).ToList();
        var selectedMC = mcList.Take(takeMC).ToList();

        quizList.Clear();

        // ✅ 先出是非題
        quizList.AddRange(selectedTF);

        // ✅ 再出選擇題
        quizList.AddRange(selectedMC);
    }

    void SetupButtonEvents()
    {
        btnA.onClick.RemoveAllListeners();
        btnB.onClick.RemoveAllListeners();
        btnC.onClick.RemoveAllListeners();
        btnD.onClick.RemoveAllListeners();
        btnO.onClick.RemoveAllListeners();
        btnX.onClick.RemoveAllListeners();

        btnA.onClick.AddListener(() => OnAnswerClicked("A"));
        btnB.onClick.AddListener(() => OnAnswerClicked("B"));
        btnC.onClick.AddListener(() => OnAnswerClicked("C"));
        btnD.onClick.AddListener(() => OnAnswerClicked("D"));

        btnO.onClick.AddListener(() => OnAnswerClicked("O"));
        btnX.onClick.AddListener(() => OnAnswerClicked("X"));

        // ★ 新增：下一題按鈕事件
        if (btnNext != null)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(OnNextButtonClicked);
            btnNext.gameObject.SetActive(false);  // 一開始先隱藏
        }
    }

    void ShowCurrentQuestion()
    {
        if (txtExplanation != null)
            txtExplanation.text = "";

        isAnswered = false;

        // 確保下一題按鈕在出題時是關閉的
        if (btnNext != null)
            btnNext.gameObject.SetActive(false);

        if (quizList == null || quizList.Count == 0)
        {
            if (txtQuestion != null)
                txtQuestion.text = "題庫為空，請確認 JSON 檔內容。";
            HideAllAnswerButtons();
            return;
        }

        if (currentIndex >= quizList.Count)
        {
            if (txtQuestion != null)
                txtQuestion.text = $"測驗結束！總分：{score} / {quizList.Count * SCORE_PER_QUESTION}";
            HideAllAnswerButtons();
            return;
        }

        var q = quizList[currentIndex];

        if (txtQuestion != null)
            txtQuestion.text = $"第{currentIndex + 1}題：{q.questionText}";

        Debug.Log($"[ShowCurrentQuestion] 題號: {q.questionNumber}, 類型: {q.kind}, 題目: {q.questionText}");

        if (q.kind == QuestionKind.MultipleChoice)
            ShowChoiceUI(q);
        else
            ShowTrueFalseUI(q);
    }

    void ShowChoiceUI(QuizQuestionRuntime q)
    {
        btnA.gameObject.SetActive(true);
        btnB.gameObject.SetActive(true);
        btnC.gameObject.SetActive(true);
        btnD.gameObject.SetActive(true);

        btnO.gameObject.SetActive(false);
        btnX.gameObject.SetActive(false);

        if (txtA != null) txtA.text = $"A. {q.optionA}";
        if (txtB != null) txtB.text = $"B. {q.optionB}";
        if (txtC != null) txtC.text = $"C. {q.optionC}";
        if (txtD != null) txtD.text = $"D. {q.optionD}";
    }

    void ShowTrueFalseUI(QuizQuestionRuntime q)
    {
        btnA.gameObject.SetActive(false);
        btnB.gameObject.SetActive(false);
        btnC.gameObject.SetActive(false);
        btnD.gameObject.SetActive(false);

        btnO.gameObject.SetActive(true);
        btnX.gameObject.SetActive(true);

        if (txtO != null) txtO.text = "O（正確）";
        if (txtX != null) txtX.text = "X（錯誤）";
    }

    void HideAllAnswerButtons()
    {
        btnA.gameObject.SetActive(false);
        btnB.gameObject.SetActive(false);
        btnC.gameObject.SetActive(false);
        btnD.gameObject.SetActive(false);
        btnO.gameObject.SetActive(false);
        btnX.gameObject.SetActive(false);

        if (btnNext != null)
            btnNext.gameObject.SetActive(false); // 測驗結束時也隱藏
    }

    void OnAnswerClicked(string chosen)
    {
        if (isAnswered) return;
        isAnswered = true;

        var q = quizList[currentIndex];
        string correct = q.correctAnswer;

        bool isCorrect = chosen.ToUpper() == correct;

        if (isCorrect)
        {
            score += SCORE_PER_QUESTION;
            if (txtExplanation != null)
                txtExplanation.text = $"✅ 答對了！\n{q.explanation}";
        }
        else
        {
            if (txtExplanation != null)
                txtExplanation.text = $"❌ 答錯了，正確答案是 {correct}\n{q.explanation}";
        }

        UpdateScoreUI();

        // ★ 改成：顯示詳解後，讓「下一題」按鈕出現，由玩家自行按下一題
        if (btnNext != null)
            btnNext.gameObject.SetActive(true);
    }

    // ★ 新增：下一題按鈕的事件處理
    void OnNextButtonClicked()
    {
        // 確保已經作答才可以按下一題
        if (!isAnswered) return;

        if (btnNext != null)
            btnNext.gameObject.SetActive(false);

        GoNextQuestion();
    }

    void GoNextQuestion()
    {
        currentIndex++;
        ShowCurrentQuestion();
    }

    void UpdateScoreUI()
    {
        if (txtScore != null)
            txtScore.text = $"分數：{score}";
    }

    void Shuffle<T>(IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
