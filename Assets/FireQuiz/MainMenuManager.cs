using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panel 參照")]
    public GameObject panelMainMenu;      // 主選單
    public GameObject panelQuizNotice;    // ★ 新增：作答須知面板
    public GameObject panelQuiz;          // 測驗畫面
    public GameObject panelHistoryList;   // 測驗紀錄列表

    [Header("按鈕 - 主選單")]
    public Button btnStartQuiz;           // 主選單：開始測驗
    public Button btnHistory;             // 主選單：測驗紀錄

    [Header("按鈕 - 作答須知")]
    public Button btnQuizNoticeStart;     // 作答須知面板上的「開始測驗」

    [Header("其它管理器")]
    public FireQuizManager quizManager;
    public HistoryListManager historyListManager;

    void Start()
    {
        // 綁定主選單按鈕
        if (btnStartQuiz != null)
        {
            btnStartQuiz.onClick.RemoveAllListeners();
            btnStartQuiz.onClick.AddListener(OnClickStartQuiz);
        }

        if (btnHistory != null)
        {
            btnHistory.onClick.RemoveAllListeners();
            btnHistory.onClick.AddListener(OnClickHistory);
        }

        // 綁定作答須知面板上的「開始測驗」按鈕
        if (btnQuizNoticeStart != null)
        {
            btnQuizNoticeStart.onClick.RemoveAllListeners();
            btnQuizNoticeStart.onClick.AddListener(OnClickQuizNoticeStart);
        }

        // 一開始先顯示主選單
        ShowMainMenuOnly();
    }

    void ShowMainMenuOnly()
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
        if (panelQuizNotice != null) panelQuizNotice.SetActive(false);
        if (panelQuiz != null) panelQuiz.SetActive(false);
        if (panelHistoryList != null) panelHistoryList.SetActive(false);
    }

    void ShowQuizNoticeOnly()
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelQuizNotice != null) panelQuizNotice.SetActive(true);
        if (panelQuiz != null) panelQuiz.SetActive(false);
        if (panelHistoryList != null) panelHistoryList.SetActive(false);
    }

    void ShowQuizOnly()
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelQuizNotice != null) panelQuizNotice.SetActive(false);
        if (panelQuiz != null) panelQuiz.SetActive(true);
        if (panelHistoryList != null) panelHistoryList.SetActive(false);
    }

    void ShowHistoryOnly()
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelQuizNotice != null) panelQuizNotice.SetActive(false);
        if (panelQuiz != null) panelQuiz.SetActive(false);
        if (panelHistoryList != null) panelHistoryList.SetActive(true);
    }

    // 主選單 → 開始測驗（只是先切到作答須知）
    void OnClickStartQuiz()
    {
        ShowQuizNoticeOnly();
    }

    // 作答須知面板 → 開始測驗
    void OnClickQuizNoticeStart()
    {
        ShowQuizOnly();

        if (quizManager != null)
        {
            // 這裡才真正初始化題目與分數
            quizManager.InitQuiz();
        }
    }

    // 主選單 → 測驗紀錄
    void OnClickHistory()
    {
        if (historyListManager != null)
        {
            historyListManager.RefreshList();
        }

        ShowHistoryOnly();
    }
}
