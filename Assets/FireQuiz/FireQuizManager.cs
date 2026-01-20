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

    [Header("UI - 導覽按鈕")]
    public Button btnNext;        // 顯示詳解後才出現的「下一題」
    public Button btnBackHome;   // 回首頁

    [Header("UI - Panel 參照")]
    public GameObject panelMainMenu; // 首頁 Panel
    public GameObject panelQuiz;     // 測驗 Panel（通常就是這個物件）

    [Header("UI - 計時")]
    public Text txtTimer;            // 顯示倒數時間
    [SerializeField] private Image imgTimerFill;   // 拖你的圓形 Image 進來
    public float quizTimeLimit = 60f; // 整份測驗限時（秒）

    [Header("玩家移動鎖定")]
    public MonoBehaviour[] componentsToDisable;

    const int QUESTIONS_PER_TYPE = 3;
    const int SCORE_PER_QUESTION = 10;

    List<QuizQuestionRuntime> quizList = new List<QuizQuestionRuntime>();
    List<QuestionRecord> currentRecords = new List<QuestionRecord>();   // 本次測驗所有題目紀錄

    int currentIndex = 0;
    int score = 0;
    bool isAnswered = false;

    float timeLeft;
    bool isTimeUp = false;
    bool quizFinished = false;   // 正常做完或時間到，設成 true

    // 建議：Start 不要自動 Init，由 MainMenuManager 按「開始測驗」時呼叫 InitQuiz
    void Start()
    {
        // 留白或註解掉原本的 InitQuiz();
        // InitQuiz();
    }

    // 給 MainMenuManager 呼叫

    public void InitQuiz()

    {
        LockMovement();
        currentRecords = new List<QuestionRecord>();
        quizFinished = false;
        isTimeUp = false;

        LoadQuestions();
        SetupButtonEvents();
        score = 0;
        currentIndex = 0;
        isAnswered = false;

        timeLeft = quizTimeLimit;
        UpdateScoreUI();
        UpdateTimerUI();

        ShowCurrentQuestion();
    }

    void Update()
    {
        // 沒開始題目、或已經結束就不跑計時
        if (quizFinished) return;
        if (quizList == null || quizList.Count == 0) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            OnTimeUp();
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (txtTimer != null)
        {
            int sec = Mathf.CeilToInt(timeLeft);
            if (sec < 0) sec = 0;
            txtTimer.text = $"{sec}";
        }

        if (imgTimerFill != null && quizTimeLimit > 0f)
        {
            imgTimerFill.fillAmount = Mathf.Clamp01(timeLeft / quizTimeLimit);
            // 如果你想「時間越少越滿」就改成：
            // imgTimerFill.fillAmount = 1f - Mathf.Clamp01(timeLeft / totalTime);
        }
    }

    void OnTimeUp()
    {
        if (quizFinished) return; // 避免重複執行

        isTimeUp = true;
        quizFinished = true;

        // 將尚未作答的題目補成「未作答」（0 分）
        // 目前流程下：currentIndex == 已作答題數
        for (int i = currentIndex; i < quizList.Count; i++)
        {
            var q = quizList[i];
            QuestionRecord record = new QuestionRecord
            {
                kind = q.kind,
                questionText = q.questionText,
                optionA = q.optionA,
                optionB = q.optionB,
                optionC = q.optionC,
                optionD = q.optionD,
                chosenAnswer = "",                // 未作答
                correctAnswer = q.correctAnswer,
                explanation = q.explanation
            };
            currentRecords.Add(record);
        }

        // 顯示時間到訊息
        if (txtQuestion != null)
            txtQuestion.text = $"時間到！測驗結束。\n總分：{score} / {quizList.Count * SCORE_PER_QUESTION}";

        HideAllAnswerButtons();

        // 存成一筆測驗紀錄
        QuizResult result = new QuizResult
        {
            dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            score = score,
            totalQuestions = quizList.Count,
            questions = new List<QuestionRecord>(currentRecords)
        };
        QuizHistoryStorage.AddResult(result);
    }

    void LoadQuestions()
    {
        if (multipleChoiceJson == null || trueFalseJson == null)
        {
            Debug.LogError("請在 Inspector 指定 multipleChoiceJson / trueFalseJson");
            return;
        }

        string mcRaw = multipleChoiceJson.text;
        string tfRaw = trueFalseJson.text;

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

        int takeMC = Mathf.Min(10-QUESTIONS_PER_TYPE, mcList.Count);
        int takeTF = Mathf.Min(QUESTIONS_PER_TYPE, tfList.Count);

        var selectedTF = tfList.Take(takeTF).ToList();
        var selectedMC = mcList.Take(takeMC).ToList();

        quizList.Clear();

        // 先是非，再選擇
        quizList.AddRange(selectedTF);
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

        if (btnNext != null)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(OnNextButtonClicked);
            btnNext.gameObject.SetActive(false);
        }

        if (btnBackHome != null)
        {
            btnBackHome.onClick.RemoveAllListeners();
            btnBackHome.onClick.AddListener(OnBackHomeClicked);
        }
    }

    void ShowCurrentQuestion()
    {
        if (quizFinished) return;  // 已經時間到或正常結束就不要再出題

        if (txtExplanation != null)
            txtExplanation.text = "";

        isAnswered = false;

        if (btnNext != null)
            btnNext.gameObject.SetActive(false);

        if (quizList == null || quizList.Count == 0)
        {
            if (txtQuestion != null)
                txtQuestion.text = "題庫為空，請確認 JSON 檔內容。";
            HideAllAnswerButtons();
            return;
        }

        // 正常作完所有題目（尚未時間到）
        if (currentIndex >= quizList.Count)
        {
            FinishQuizNormally();
            return;
        }

        var q = quizList[currentIndex];

        if (txtQuestion != null)
            txtQuestion.text = $"第{currentIndex + 1}/10題：{q.questionText}";

        Debug.Log($"[ShowCurrentQuestion] 題號: {q.questionNumber}, 類型: {q.kind}, 題目: {q.questionText}");

        if (q.kind == QuestionKind.MultipleChoice)
            ShowChoiceUI(q);
        else
            ShowTrueFalseUI(q);
    }

    void FinishQuizNormally()
    {
        if (quizFinished) return;
        quizFinished = true;

        if (txtQuestion != null)
            txtQuestion.text = $"測驗結束！總分：{score} / 100";

        HideAllAnswerButtons();

        // 理論上 currentRecords.Count == quizList.Count，如果怕有差，可以補齊
        if (currentRecords.Count < quizList.Count)
        {
            for (int i = currentRecords.Count; i < quizList.Count; i++)
            {
                var q = quizList[i];
                QuestionRecord record = new QuestionRecord
                {
                    kind = q.kind,
                    questionText = q.questionText,
                    optionA = q.optionA,
                    optionB = q.optionB,
                    optionC = q.optionC,
                    optionD = q.optionD,
                    chosenAnswer = "",            // 當未作答處理
                    correctAnswer = q.correctAnswer,
                    explanation = q.explanation
                };
                currentRecords.Add(record);
            }
        }

        QuizResult result = new QuizResult
        {
            dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            score = score,
            totalQuestions = quizList.Count,
            questions = new List<QuestionRecord>(currentRecords)
        };
        QuizHistoryStorage.AddResult(result);
    }

    // ★ 有些題目只有 3 個選項，空的就隱藏按鈕
    void ShowChoiceUI(QuizQuestionRuntime q)
    {
        if (btnO != null) btnO.gameObject.SetActive(false);
        if (btnX != null) btnX.gameObject.SetActive(false);

        bool hasA = !string.IsNullOrWhiteSpace(q.optionA);
        bool hasB = !string.IsNullOrWhiteSpace(q.optionB);
        bool hasC = !string.IsNullOrWhiteSpace(q.optionC);
        bool hasD = !string.IsNullOrWhiteSpace(q.optionD);

        if (btnA != null)
        {
            btnA.gameObject.SetActive(hasA);
            if (hasA && txtA != null)
                txtA.text = $"A. {q.optionA}";
        }

        if (btnB != null)
        {
            btnB.gameObject.SetActive(hasB);
            if (hasB && txtB != null)
                txtB.text = $"B. {q.optionB}";
        }

        if (btnC != null)
        {
            btnC.gameObject.SetActive(hasC);
            if (hasC && txtC != null)
                txtC.text = $"C. {q.optionC}";
        }

        if (btnD != null)
        {
            btnD.gameObject.SetActive(hasD);
            if (hasD && txtD != null)
                txtD.text = $"D. {q.optionD}";
        }
    }

    void ShowTrueFalseUI(QuizQuestionRuntime q)
    {
        if (btnA != null) btnA.gameObject.SetActive(false);
        if (btnB != null) btnB.gameObject.SetActive(false);
        if (btnC != null) btnC.gameObject.SetActive(false);
        if (btnD != null) btnD.gameObject.SetActive(false);

        if (btnO != null) btnO.gameObject.SetActive(true);
        if (btnX != null) btnX.gameObject.SetActive(true);

        if (txtO != null) txtO.text = "O（正確）";
        if (txtX != null) txtX.text = "X（錯誤）";
    }

    void HideAllAnswerButtons()
    {
        if (btnA != null) btnA.gameObject.SetActive(false);
        if (btnB != null) btnB.gameObject.SetActive(false);
        if (btnC != null) btnC.gameObject.SetActive(false);
        if (btnD != null) btnD.gameObject.SetActive(false);
        if (btnO != null) btnO.gameObject.SetActive(false);
        if (btnX != null) btnX.gameObject.SetActive(false);

        if (btnNext != null)
            btnNext.gameObject.SetActive(false);
    }

    void OnAnswerClicked(string chosen)
    {
        // 已作答、已時間到或測驗已結束都不接受
        if (isAnswered || quizFinished || isTimeUp) return;
        isAnswered = true;

        var q = quizList[currentIndex];
        string correct = q.correctAnswer;
        bool isCorrect = chosen.ToUpper() == correct;

        // 記錄這一題
        QuestionRecord record = new QuestionRecord
        {
            kind = q.kind,
            questionText = q.questionText,
            optionA = q.optionA,
            optionB = q.optionB,
            optionC = q.optionC,
            optionD = q.optionD,
            chosenAnswer = chosen.ToUpper(),
            correctAnswer = correct,
            explanation = q.explanation
        };
        currentRecords.Add(record);

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

        if (btnNext != null)
            btnNext.gameObject.SetActive(true);
    }

    void OnNextButtonClicked()
    {
        if (!isAnswered || quizFinished || isTimeUp) return;

        if (btnNext != null)
            btnNext.gameObject.SetActive(false);

        currentIndex++;
        ShowCurrentQuestion();
    }

    void OnBackHomeClicked()
    {
        UnlockMovement();

        if (panelQuiz != null) panelQuiz.SetActive(false);
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
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
    void SetMovementEnabled(bool enabled)
    {
        if (componentsToDisable == null) return;

        foreach (var comp in componentsToDisable)
        {
            if (comp == null) continue;
            comp.enabled = enabled;
        }
    }

    void LockMovement()
    {
        SetMovementEnabled(false);
    }

    void UnlockMovement()
    {
        SetMovementEnabled(true);
    }
}
