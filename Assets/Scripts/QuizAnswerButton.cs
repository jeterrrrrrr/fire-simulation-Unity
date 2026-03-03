using UnityEngine;

public class QuizAnswerButton : MonoBehaviour
{
    [Header("設定")]
    public bool isCorrectAnswer = false;   // 勾選 = 正確答案
    public GameObject quizUI;              // 整個答題 UI

    public void OnButtonClicked()
    {
        if (isCorrectAnswer)
        {
            // ★ 答對 → 計數 +1
            GameProgressManager.Instance.AddFoundDanger();
        }

        // 不論對錯，都關閉答題 UI
        if (quizUI)
            quizUI.SetActive(false);
    }
}
