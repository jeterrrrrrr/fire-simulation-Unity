using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // ★ 1. 必須加入這行才能切換場景

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject startUI;
    public GameObject failureUI;

    [Header("Player Control")]
    public GameObject locomotionSystem;
    public GameObject leftController;

    [Header("Timer UI")]
    public Image radialTimer;
    public Text timerText; // 若使用 TMPro 請自行更改

    [Header("Timer Settings")]
    public int timeLimit = 180;
    private int currentTime; // 用來記錄動態時間

    void Start()
    {
        // 確保重新開始時，時間重置
        currentTime = timeLimit; 
        
        // 初始狀態
        if (failureUI != null) failureUI.SetActive(false);
        startUI.SetActive(true);
        
        // 剛開始不能動
        locomotionSystem.SetActive(false);
        leftController.SetActive(false);

        if (timerText != null) timerText.text = currentTime.ToString();
    }

    public void StartGame()
    {
        locomotionSystem.SetActive(true);
        leftController.SetActive(true);
        startUI.SetActive(false);
        
        // 確保每次開始都重置計時器
        currentTime = timeLimit;
        InvokeRepeating("timer", 1, 1);
    }

    void timer()
    {
        currentTime -= 1; // 使用內部變數計算
        if (timerText != null) timerText.text = currentTime.ToString();

        float progress = Mathf.Clamp01(((float)currentTime) / (float)timeLimit);
        if (radialTimer != null) radialTimer.fillAmount = progress;
        
        if (currentTime <= 0)
        {
            currentTime = 0;
            TimeUp();
        }
    }

    void TimeUp()
    {
        Debug.Log("時間到！");
        CancelInvoke("timer");

        // 停止移動
        if (locomotionSystem != null) locomotionSystem.SetActive(false);
        // 顯示失敗畫面
        if (failureUI != null) failureUI.SetActive(true);
    }

    // ★ 2. 新增這個函式：重新載入當前場景
    public void RestartGame()
    {
        // 讀取目前正在執行的場景名稱，並重新載入它
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}