using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // ★新增

public class LevelPagerLegacyUI : MonoBehaviour
{
    [Serializable]
    public class LevelInfo
    {
        [Header("圖片")]
        public Sprite sprite; // 直接拖 Sprite

        [Header("文字")]
        public string title;

        [TextArea(3, 10)]
        public string description;

        [Header("要切換的 Scene 名稱（需加入 Build Settings）")]
        public string sceneName; // 例如 "10_Level01"
    }

    [Header("關卡資料（放 6 筆）")]
    public List<LevelInfo> levels = new List<LevelInfo>(6);

    [Header("UI 參考（Legacy UI）")]
    public Text txtTitle;
    public Text txtDescription;
    public GameObject txtStageStatus;   // ✅ 保留 GameObject（你原本就是這樣）
    public Image imgLevel;
    public Button btnPrev;
    public Button btnNext;

    [Header("開始按鈕")]
    public Button btnStart;

    [Header("設定")]
    [Tooltip("是否在第一關禁用上一關、最後一關禁用下一關")]
    public bool disableButtonsAtEdges = true;

    [Header("狀態文字")]
    public string notClearedText = "未通關";
    public string clearedText = "已通關";

    [Header("綜合測驗設定（預設最後兩關）")]
    public int integratedTailCount = 2; // 後兩關是綜合測驗

    private int currentIndex = 0;

    // ✅ 內部抓到的 Text（用來改 txtStageStatus 的內容）
    private Text txtStageStatusText;

    private void Awake()
    {
        if (btnPrev != null) btnPrev.onClick.AddListener(Prev);
        if (btnNext != null) btnNext.onClick.AddListener(Next);

        if (btnStart != null) btnStart.onClick.AddListener(StartLevel);

        // ✅ 抓 txtStageStatus 上的 Text（避免你 Inspector 重新拉）
        if (txtStageStatus != null)
        {
            txtStageStatusText = txtStageStatus.GetComponent<Text>();
            if (txtStageStatusText == null)
                txtStageStatusText = txtStageStatus.GetComponentInChildren<Text>(true);

            if (txtStageStatusText == null)
                Debug.LogWarning("[LevelPagerLegacyUI] txtStageStatus 找不到 Text 元件，無法更新狀態文字。");
        }
    }

    private void Start()
    {
        // 如果你希望狀態永遠顯示，就確保開著
        if (txtStageStatus != null) txtStageStatus.SetActive(true);

        ShowLevel(0); // 一開始第一關
    }

    private bool IsLevelUnlocked(int index)
    {
        int total = (levels != null) ? levels.Count : 0;
        int tail = Mathf.Clamp(integratedTailCount, 0, total);
        int baseCount = total - tail;      // 例如 6-2=4
        int integratedStart = baseCount;   // 後兩關起點 index=4

        // 前 4 關永遠視為可進入
        if (index < integratedStart) return true;

        // 後兩關：需要 4/4 才解鎖
        return GameState.ClearedCount() >= GameState.LevelCount; // 4/4
    }


    public void Next()
    {
        if (levels == null || levels.Count == 0) return;
        int nextIndex = Mathf.Clamp(currentIndex + 1, 0, levels.Count - 1);
        ShowLevel(nextIndex);
    }

    public void Prev()
    {
        if (levels == null || levels.Count == 0) return;
        int prevIndex = Mathf.Clamp(currentIndex - 1, 0, levels.Count - 1);
        ShowLevel(prevIndex);
    }

    public void ShowLevel(int index)
    {
        if (levels == null || levels.Count == 0) return;
        if (index < 0 || index >= levels.Count) return;

        currentIndex = index;
        LevelInfo info = levels[currentIndex];

        // 標題/說明
        if (txtTitle != null) txtTitle.text = info.title ?? "";
        if (txtDescription != null) txtDescription.text = info.description ?? "";

        // 圖片
        if (imgLevel != null)
        {
            imgLevel.sprite = info.sprite;
            imgLevel.enabled = (info.sprite != null);
        }

        // 按鈕狀態
        if (disableButtonsAtEdges)
        {
            if (btnPrev != null) btnPrev.interactable = (currentIndex > 0);
            if (btnNext != null) btnNext.interactable = (currentIndex < levels.Count - 1);
        }
        else
        {
            if (btnPrev != null) btnPrev.interactable = true;
            if (btnNext != null) btnNext.interactable = true;
        }

        // sceneName 沒填就禁用開始
        if (btnStart != null)
        {
            bool sceneOk = !string.IsNullOrWhiteSpace(info.sceneName);
            bool unlocked = IsLevelUnlocked(currentIndex);
            btnStart.interactable = sceneOk && unlocked; // ✅ 未解鎖就灰色不能按
        }


        // ✅ 更新通關/進度顯示（改用新的 GameState）
        UpdateStageStatus();
    }

    // ✅ 核心：改成讀 GameState
    private void UpdateStageStatus()
    {
        if (txtStageStatusText == null) return;

        int total = (levels != null) ? levels.Count : 0;
        int tail = Mathf.Clamp(integratedTailCount, 0, total);
        int baseCount = total - tail;           // 例如 6-2=4
        int integratedStart = baseCount;        // 後兩關起點 index=4

        // 保險：如果你的 levels 不是 6 筆，也不會爆
        if (total == 0)
        {
            txtStageStatusText.text = "";
            return;
        }

        // 第 1~4 關（index 0~3）：顯示 未通關/已通關
        if (currentIndex < integratedStart)
        {
            int levelNumber = currentIndex + 1; // Level1~Level4
            bool cleared = GameState.IsCleared(levelNumber);
            txtStageStatusText.text = cleared ? clearedText : notClearedText;
        }
        else
        {
            // 後兩關（綜合測驗）：顯示 x/4
            // 直接用 GameState 的統計（Level1~4）
            int c = GameState.ClearedCount();
            if (c == 4)
            {
                txtStageStatusText.text = $"{c}/{GameState.LevelCount}已解鎖";
            }
            else
            {
                txtStageStatusText.text = $"{c}/{GameState.LevelCount}未解鎖";
            }
            
        }
    }

    // 按開始切換 Scene
    public void StartLevel()
    {
        if (levels == null || levels.Count == 0) return;

        string scene = levels[currentIndex].sceneName;
        if (string.IsNullOrWhiteSpace(scene))
        {
            Debug.LogError($"[LevelPagerLegacyUI] 第 {currentIndex + 1} 關的 sceneName 沒填。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(scene.Trim(), LoadSceneMode.Single);
    }
}
