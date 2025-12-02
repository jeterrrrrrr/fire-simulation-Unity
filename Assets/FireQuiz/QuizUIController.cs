using UnityEngine;

public class QuizUIController : MonoBehaviour
{
    public GameObject panelMainMenu;
    public GameObject panelQuiz;
    public GameObject panelHistoryList;
    public GameObject panelHistoryDetail;

    public FireQuizManager quizManager;
    public HistoryListManager historyListManager;
    public HistoryDetailManager historyDetailManager;

    void Start()
    {
        ShowMainMenu();
    }

    void ShowOnly(GameObject target)
    {
        panelMainMenu.SetActive(target == panelMainMenu);
        panelQuiz.SetActive(target == panelQuiz);
        panelHistoryList.SetActive(target == panelHistoryList);
        panelHistoryDetail.SetActive(target == panelHistoryDetail);
    }

    public void ShowMainMenu()
    {
        ShowOnly(panelMainMenu);
    }

    public void OnClickStartQuiz()
    {
        // 這邊也可以順便叫 quizManager 重新 Init
        ShowOnly(panelQuiz);
        //quizManager.InitQuizFromOutside(); // 下面說明怎麼加（或你直接叫 StartQuiz 之類）
    }

    public void OnClickHistory()
    {
        // 打開紀錄列表前，叫它重新讀資料
        historyListManager.RefreshList();
        ShowOnly(panelHistoryList);
    }

    public void ShowHistoryDetail(QuizResult result)
    {
        historyDetailManager.ShowResult(result);
        ShowOnly(panelHistoryDetail);
    }
}

