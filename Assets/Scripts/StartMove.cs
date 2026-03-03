using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject startUI;
    public GameObject failureUI;
    public GameObject successUI;   // ★ 成功畫面
    public GameObject hudPanel;    // ★ HUD（計時顯示）

    [Header("Player Control")]
    public GameObject locomotionSystem;
    public GameObject leftController;

    [Header("Timer UI")]
    public Image radialTimer;
    public Text timerText;

    [Header("Timer Settings")]
    public int timeLimit = 180;
    private int currentTime;
    private bool gameEnded = false;

    void Start()
    {
        currentTime = timeLimit;

        if (startUI) startUI.SetActive(true);
        if (failureUI) failureUI.SetActive(false);
        if (successUI) successUI.SetActive(false);
        if (hudPanel) hudPanel.SetActive(false);

        // 一開始不能動
        if (locomotionSystem) locomotionSystem.SetActive(false);
        if (leftController) leftController.SetActive(false);

        UpdateTimerUI();
    }

    public void StartGame()
    {
        gameEnded = false;

        if (startUI) startUI.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);

        if (locomotionSystem) locomotionSystem.SetActive(true);
        if (leftController) leftController.SetActive(true);

        currentTime = timeLimit;
        UpdateTimerUI();

        CancelInvoke();
        InvokeRepeating(nameof(TimerTick), 1f, 1f);
    }

    void TimerTick()
    {
        if (gameEnded) return;

        currentTime--;

        UpdateTimerUI();

        if (currentTime <= 0)
        {
            currentTime = 0;
            TimeUp();
        }
    }

    void UpdateTimerUI()
    {
        if (timerText)
            timerText.text = currentTime.ToString();

        if (radialTimer)
            radialTimer.fillAmount = Mathf.Clamp01((float)currentTime / timeLimit);
    }

    void TimeUp()
    {
        gameEnded = true;
        CancelInvoke();

        Debug.Log("❌ 時間到，任務失敗");

        if (locomotionSystem) locomotionSystem.SetActive(false);
        if (leftController) leftController.SetActive(false);

        if (hudPanel) hudPanel.SetActive(false);
        if (failureUI) failureUI.SetActive(true);
    }

    // ⭐ 由「通關判斷（找到4個危險源）」呼叫
    public void GameSuccess()
    {
        if (gameEnded) return;

        gameEnded = true;
        CancelInvoke();

        Debug.Log("🎉 任務成功");

        if (locomotionSystem) locomotionSystem.SetActive(false);
        if (leftController) leftController.SetActive(false);

        if (hudPanel) hudPanel.SetActive(false);
        if (successUI) successUI.SetActive(true);
    }

    // 重新開始
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
