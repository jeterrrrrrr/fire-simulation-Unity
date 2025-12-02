using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HistoryListManager : MonoBehaviour
{
    [Header("UI 參照")]
    public Transform contentRoot;         // ScrollView/Viewport/Content
    public GameObject historyItemPrefab;  // 一列紀錄的 prefab
    public QuizUIController uiController; // 用來切換到詳情頁

    List<QuizResult> cachedResults = new List<QuizResult>();

    // 給 QuizUIController 呼叫
    public void RefreshList()
    {
        // 清空舊的 item
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        var wrapper = QuizHistoryStorage.LoadAll();
        cachedResults = wrapper.results;

        if (cachedResults == null || cachedResults.Count == 0)
        {
            // 你可以在這裡顯示「目前沒有紀錄」的字樣
            return;
        }

        // 一筆紀錄產生一個 item
        foreach (var result in cachedResults)
        {
            GameObject go = Instantiate(historyItemPrefab, contentRoot);

            // 找到底下的 Text（建議你在 prefab 上把名稱改成 TxtDateTime / TxtScore）
            Text[] texts = go.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = result.dateTime;
                texts[1].text = $"{result.score} 分（{result.totalQuestions} 題）";
            }


            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                var capturedResult = result; // 避免閉包問題
                btn.onClick.AddListener(() =>
                {
                    uiController.ShowHistoryDetail(capturedResult);
                });
            }
        }
    }
}
