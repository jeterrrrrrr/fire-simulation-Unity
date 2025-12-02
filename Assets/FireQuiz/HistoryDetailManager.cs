using UnityEngine;
using UnityEngine.UI;

public class HistoryDetailManager : MonoBehaviour
{
    [Header("UI 文字")]
    public Text txtHeader;          // ex: "2025-11-25 22:30  分數：80 / 100"
    public Text txtQuestion;        // 題目
    public Text txtOptions;         // 選項或 O/X 說明
    public Text txtYourAnswer;      // 你的答案
    public Text txtCorrectAnswer;   // 正確答案
    public Text txtExplanation;     // 詳解

    [Header("UI 按鈕")]
    public Button btnPrev;
    public Button btnNext;
    public Button btnBack;

    public QuizUIController uiController;

    QuizResult currentResult;
    int currentIndex = 0;

    void Start()
    {
        if (btnPrev != null) btnPrev.onClick.AddListener(OnPrevClicked);
        if (btnNext != null) btnNext.onClick.AddListener(OnNextClicked);
        if (btnBack != null) btnBack.onClick.AddListener(OnBackClicked);
    }

    // 給 QuizUIController 呼叫
    public void ShowResult(QuizResult result)
    {
        currentResult = result;
        currentIndex = 0;
        UpdateQuestionUI();
    }

    void UpdateQuestionUI()
    {
        if (currentResult == null || currentResult.questions == null || currentResult.questions.Count == 0)
        {
            if (txtQuestion != null) txtQuestion.text = "沒有題目資料。";
            return;
        }

        if (currentIndex < 0) currentIndex = 0;
        if (currentIndex >= currentResult.questions.Count) currentIndex = currentResult.questions.Count - 1;

        var q = currentResult.questions[currentIndex];

        // 標題：日期 + 分數 + 題目進度
        if (txtHeader != null)
        {
            txtHeader.text =
                $"{currentResult.dateTime}  分數：{currentResult.score} / {(currentResult.totalQuestions * 10)}\n" +
                $"第 {currentIndex + 1} 題 / 共 {currentResult.questions.Count} 題";
        }

        if (txtQuestion != null)
        {
            txtQuestion.text = q.questionText;
        }

        // 顯示選項
        if (txtOptions != null)
        {
            if (q.kind == QuestionKind.MultipleChoice)
            {
                txtOptions.text =
                    $"A. {q.optionA}\n" +
                    $"B. {q.optionB}\n" +
                    $"C. {q.optionC}\n" +
                    $"D. {q.optionD}";
            }
            else
            {
                txtOptions.text = "O（正確）\nX（錯誤）";
            }
        }

        // 顯示你的答案 & 正確答案（文字做成比較友善）
        if (txtYourAnswer != null)
        {
            txtYourAnswer.text = "你的答案：" + FormatAnswer(q.chosenAnswer, q);
        }

        if (txtCorrectAnswer != null)
        {
            txtCorrectAnswer.text = "正確答案：" + FormatAnswer(q.correctAnswer, q);
        }

        if (txtExplanation != null)
        {
            txtExplanation.text = q.explanation;
        }

        // 按鈕是否可按
        if (btnPrev != null)
            btnPrev.interactable = currentIndex > 0;
        if (btnNext != null)
            btnNext.interactable = currentIndex < currentResult.questions.Count - 1;
    }

    string FormatAnswer(string code, QuestionRecord q)
    {
        if (q.kind == QuestionKind.MultipleChoice)
        {
            switch (code)
            {
                case "A": return $"A. {q.optionA}";
                case "B": return $"B. {q.optionB}";
                case "C": return $"C. {q.optionC}";
                case "D": return $"D. {q.optionD}";
                default: return code;
            }
        }
        else
        {
            if (code == "O") return "O（正確）";
            if (code == "X") return "X（錯誤）";
            return code;
        }
    }

    void OnPrevClicked()
    {
        if (currentResult == null) return;
        currentIndex--;
        UpdateQuestionUI();
    }

    void OnNextClicked()
    {
        if (currentResult == null) return;
        currentIndex++;
        UpdateQuestionUI();
    }

    void OnBackClicked()
    {
        // 回到測驗紀錄列表
        uiController.OnClickHistory();
    }
}
