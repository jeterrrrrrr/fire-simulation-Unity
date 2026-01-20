using UnityEngine;
using UnityEngine.UI;

public class HistoryDetailManager : MonoBehaviour
{
    [Header("UI 文字")]
    public Text txtHeader;          // ex: "2025-11-25 22:30  分數：80 / 100"
    public Text txtQuestion;        // 題目
    public Text txtYourAnswer;      // 你的答案
    public Text txtCorrectAnswer;   // 正確答案
    public Text txtExplanation;     // 詳解

    [Header("UI 按鈕 - 選擇題")]
    public Button btnA;
    public Text txtA;
    public Button btnB;
    public Text txtB;
    public Button btnC;
    public Text txtC;
    public Button btnD;
    public Text txtD;

    [Header("UI 按鈕 - 是非題")]
    public Button btnO;
    public Text txtO;
    public Button btnX;
    public Text txtX;

    [Header("UI 導覽按鈕")]
    public Button btnPrev;
    public Button btnNext;
    public Button btnBackToList;

    [Header("Panel 參照")]
    public GameObject panelHistoryList;
    public GameObject panelHistoryDetail;

    QuizResult currentResult;
    int currentIndex = 0;

    // 顏色：預設 / 答對 / 答錯 / 未作答正解（深灰）
    Color defaultChoiceColor = Color.white;
    Color defaultTFColor = Color.white;
    bool hasDefaultChoiceColor = false;
    bool hasDefaultTFColor = false;

    Color colorCorrect = Color.green;
    Color colorWrong = Color.red;
    Color colorUnansweredCorrect = new Color(0.3f, 0.3f, 0.3f); // 深灰色

    void Start()
    {
        if (btnPrev != null) btnPrev.onClick.AddListener(OnPrevClicked);
        if (btnNext != null) btnNext.onClick.AddListener(OnNextClicked);
        if (btnBackToList != null) btnBackToList.onClick.AddListener(OnBackClicked);

        CacheDefaultColors();
        SetButtonsInteractable(false); // 詳情頁按鈕只顯示，不讓按
    }

    void CacheDefaultColors()
    {
        if (!hasDefaultChoiceColor)
        {
            if (btnA != null)
            {
                var img = btnA.GetComponent<Image>();
                if (img != null)
                {
                    defaultChoiceColor = img.color;
                    hasDefaultChoiceColor = true;
                }
            }
        }

        if (!hasDefaultTFColor)
        {
            if (btnO != null)
            {
                var img = btnO.GetComponent<Image>();
                if (img != null)
                {
                    defaultTFColor = img.color;
                    hasDefaultTFColor = true;
                }
            }
        }
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (btnA != null) btnA.interactable = interactable;
        if (btnB != null) btnB.interactable = interactable;
        if (btnC != null) btnC.interactable = interactable;
        if (btnD != null) btnD.interactable = interactable;
        if (btnO != null) btnO.interactable = interactable;
        if (btnX != null) btnX.interactable = interactable;
    }

    void ResetAllButtonColors()
    {
        if (btnA != null)
        {
            var img = btnA.GetComponent<Image>();
            if (img != null && hasDefaultChoiceColor) img.color = defaultChoiceColor;
        }
        if (btnB != null)
        {
            var img = btnB.GetComponent<Image>();
            if (img != null && hasDefaultChoiceColor) img.color = defaultChoiceColor;
        }
        if (btnC != null)
        {
            var img = btnC.GetComponent<Image>();
            if (img != null && hasDefaultChoiceColor) img.color = defaultChoiceColor;
        }
        if (btnD != null)
        {
            var img = btnD.GetComponent<Image>();
            if (img != null && hasDefaultChoiceColor) img.color = defaultChoiceColor;
        }
        if (btnO != null)
        {
            var img = btnO.GetComponent<Image>();
            if (img != null && hasDefaultTFColor) img.color = defaultTFColor;
        }
        if (btnX != null)
        {
            var img = btnX.GetComponent<Image>();
            if (img != null && hasDefaultTFColor) img.color = defaultTFColor;
        }
    }

    // 給 HistoryListManager 呼叫
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
            int maxScore = currentResult.totalQuestions * 10; // 假設每題10分
            txtHeader.text = $"  分數：{currentResult.score} / {maxScore}";
        }

        // 題目文字
        if (txtQuestion != null)
        {
            txtQuestion.text = $"第 {currentIndex + 1}/{currentResult.questions.Count} 題: " +q.questionText;
        }

        // 重設所有按鈕顏色 & 顯示狀態
        ResetAllButtonColors();
        SetupButtonsForQuestion(q);

        // 你的答案 / 正確答案（文字）
        if (txtYourAnswer != null)
        {
            txtYourAnswer.text = "你的答案：" + FormatAnswer(q.chosenAnswer, q);
        }

        if (txtCorrectAnswer != null)
        {
            txtCorrectAnswer.text = "正確答案：" + FormatAnswer(q.correctAnswer, q);
        }

        // 詳解
        if (txtExplanation != null)
        {
            txtExplanation.text = q.explanation;
        }

        // 按鈕顏色：依 chosenAnswer / correctAnswer 上色
        ApplyButtonColors(q);

        // 上一題 / 下一題是否可按
        if (btnPrev != null)
            btnPrev.interactable = currentIndex > 0;
        if (btnNext != null)
            btnNext.interactable = currentIndex < currentResult.questions.Count - 1;
    }

    void SetupButtonsForQuestion(QuestionRecord q)
    {
        if (q.kind == QuestionKind.MultipleChoice)
        {
            // 顯示選擇題按鈕，隱藏是非題按鈕
            if (btnA != null) btnA.gameObject.SetActive(true);
            if (btnB != null) btnB.gameObject.SetActive(true);
            if (btnC != null) btnC.gameObject.SetActive(true);
            if (btnD != null) btnD.gameObject.SetActive(true);

            if (btnO != null) btnO.gameObject.SetActive(false);
            if (btnX != null) btnX.gameObject.SetActive(false);

            // 處理可能為空的選項（只顯示有內容的）
            bool hasA = !string.IsNullOrWhiteSpace(q.optionA);
            bool hasB = !string.IsNullOrWhiteSpace(q.optionB);
            bool hasC = !string.IsNullOrWhiteSpace(q.optionC);
            bool hasD = !string.IsNullOrWhiteSpace(q.optionD);

            if (btnA != null)
            {
                btnA.gameObject.SetActive(hasA);
                if (hasA && txtA != null) txtA.text = $"A. {q.optionA}";
            }

            if (btnB != null)
            {
                btnB.gameObject.SetActive(hasB);
                if (hasB && txtB != null) txtB.text = $"B. {q.optionB}";
            }

            if (btnC != null)
            {
                btnC.gameObject.SetActive(hasC);
                if (hasC && txtC != null) txtC.text = $"C. {q.optionC}";
            }

            if (btnD != null)
            {
                btnD.gameObject.SetActive(hasD);
                if (hasD && txtD != null) txtD.text = $"D. {q.optionD}";
            }
        }
        else
        {
            // 是非題：只顯示 O / X
            if (btnA != null) btnA.gameObject.SetActive(false);
            if (btnB != null) btnB.gameObject.SetActive(false);
            if (btnC != null) btnC.gameObject.SetActive(false);
            if (btnD != null) btnD.gameObject.SetActive(false);

            if (btnO != null)
            {
                btnO.gameObject.SetActive(true);
                if (txtO != null) txtO.text = "O（正確）";
            }

            if (btnX != null)
            {
                btnX.gameObject.SetActive(true);
                if (txtX != null) txtX.text = "X（錯誤）";
            }
        }

        // 詳情頁不讓再按
        SetButtonsInteractable(false);
    }

    void ApplyButtonColors(QuestionRecord q)
    {
        string chosen = (q.chosenAnswer ?? "").Trim().ToUpper();
        string correct = (q.correctAnswer ?? "").Trim().ToUpper();

        // 未作答：只標正確答案深灰色
        if (string.IsNullOrWhiteSpace(chosen))
        {
            HighlightCorrectAsUnanswered(q.kind, correct);
            return;
        }

        // 有作答
        if (chosen == correct)
        {
            // 答對：玩家選的那一個綠色
            SetButtonColorByCode(q.kind, chosen, colorCorrect);
        }
        else
        {
            // 答錯：玩家選的紅色，正確的綠色
            SetButtonColorByCode(q.kind, chosen, colorWrong);
            SetButtonColorByCode(q.kind, correct, colorCorrect);
        }
    }

    void HighlightCorrectAsUnanswered(QuestionKind kind, string correctCode)
    {
        if (string.IsNullOrWhiteSpace(correctCode))
            return;

        SetButtonColorByCode(kind, correctCode.Trim().ToUpper(), colorUnansweredCorrect);
    }

    void SetButtonColorByCode(QuestionKind kind, string code, Color color)
    {
        Button target = null;

        if (kind == QuestionKind.MultipleChoice)
        {
            switch (code)
            {
                case "A": target = btnA; break;
                case "B": target = btnB; break;
                case "C": target = btnC; break;
                case "D": target = btnD; break;
            }
        }
        else // TrueFalse
        {
            switch (code)
            {
                case "O": target = btnO; break;
                case "X": target = btnX; break;
            }
        }

        if (target == null || !target.gameObject.activeSelf) return;

        var img = target.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }
    }

    string FormatAnswer(string code, QuestionRecord q)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "（未作答）";

        string c = code.Trim().ToUpper();

        if (q.kind == QuestionKind.MultipleChoice)
        {
            switch (c)
            {
                case "A": return $"A. {q.optionA}";
                case "B": return $"B. {q.optionB}";
                case "C": return $"C. {q.optionC}";
                case "D": return $"D. {q.optionD}";
                default: return c;
            }
        }
        else
        {
            if (c == "O") return "O（正確）";
            if (c == "X") return "X（錯誤）";
            return c;
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
        if (panelHistoryDetail != null) panelHistoryDetail.SetActive(false);
        if (panelHistoryList != null) panelHistoryList.SetActive(true);
    }
}
