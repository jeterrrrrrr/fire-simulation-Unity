using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance;

    [Header("HUD UI")]
    public GameObject hudPanel;   // ★ 左上角整個 HUD
    public TextMeshProUGUI countText;
    public Text timerText;

    [Header("通關 UI")]
    public GameObject clearPanel;
    public Text timeUsedText;

    [Header("設定")]
    public int totalDangerCount = 4;

    int foundCount = 0;
    float startTime;
    bool gameCleared = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        startTime = Time.time;
        UpdateCountUI();
    }

    void Update()
    {
        if (gameCleared) return; // ★ 成功後不再更新 → 計時停止

        float timeUsed = Time.time - startTime;

        if (timerText)
            timerText.text = "時間：" + timeUsed.ToString("F1") + " 秒";
    }

    // ★ 正確答案按鈕呼叫
    public void AddFoundDanger()
    {
        if (gameCleared) return;

        foundCount++;
        UpdateCountUI();

        if (foundCount >= totalDangerCount)
        {
            GameClear();
        }
    }

    void UpdateCountUI()
    {
        if (countText)
            countText.text = $"已找到危險源：{foundCount} / {totalDangerCount}";
    }

    void GameClear()
    {
        gameCleared = true;

        float totalTime = Time.time - startTime;

        // ★ 隱藏 HUD
        if (hudPanel)
            hudPanel.SetActive(false);

        // ★ 顯示通關 UI
        if (clearPanel)
            clearPanel.SetActive(true);

        if (timeUsedText)
            timeUsedText.text = "用時：" + totalTime.ToString("F1") + " 秒";

        Debug.Log("🎉 通關完成，用時：" + totalTime);
    }
}
