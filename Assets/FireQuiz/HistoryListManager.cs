using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HistoryListManager : MonoBehaviour
{
    [Header("UI 參照")]
    public Transform contentRoot;         // ScrollView/Viewport/Content
    public GameObject historyItemPrefab;  // 一列紀錄的 prefab

    [Header("Panel 參照")]
    public GameObject panelMainMenu;
    public GameObject panelHistoryList;
    public GameObject panelHistoryDetail;

    [Header("其它管理器")]
    public HistoryDetailManager detailManager;

    [Header("按鈕")]
    public Button btnBackHome;

    List<QuizResult> cachedResults = new List<QuizResult>();

    void Start()
    {
        if (btnBackHome != null)
        {
            btnBackHome.onClick.RemoveAllListeners();
            btnBackHome.onClick.AddListener(OnBackHomeClicked);
        }
    }

    // 給外部（MainMenuManager）呼叫
    public void RefreshList()
    {
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        var wrapper = QuizHistoryStorage.LoadAll();
        cachedResults = wrapper.results;

        if (cachedResults == null || cachedResults.Count == 0)
        {
            // TODO: 你可以在這裡顯示「目前沒有紀錄」
            return;
        }

        foreach (var result in cachedResults)
        {
            GameObject go = Object.Instantiate(historyItemPrefab, contentRoot);

            // 這裡假設 prefab 底下有兩個 Text：第一個顯示日期、第二個顯示分數
            Text[] texts = go.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = result.dateTime;
                texts[1].text = $"{result.score} 分";
            }

            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                var capturedResult = result; // 避免閉包問題
                btn.onClick.AddListener(() =>
                {
                    // 點下這筆 → 切到詳情頁
                    if (detailManager != null)
                    {
                        detailManager.ShowResult(capturedResult);
                    }

                    if (panelHistoryList != null) panelHistoryList.SetActive(false);
                    if (panelHistoryDetail != null) panelHistoryDetail.SetActive(true);
                });
            }
        }
    }

    void OnBackHomeClicked()
    {
        if (panelHistoryList != null) panelHistoryList.SetActive(false);
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
    }
}
