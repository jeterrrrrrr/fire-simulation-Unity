using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;       // 新輸入系統
using UnityEngine.UI;                // Text (Legacy)
using UnityEngine.Events;            // UnityEvent
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StepTextByGrabsUI : MonoBehaviour
{
    // ===== 文字目標 =====
    [Header("Target Text (Legacy)")]
    public Text legacyText; // 可留空→自動抓本物件上的 Text

    // ===== 倒數計時文字 =====
    [Header("Timer Text (Legacy)")]
    [SerializeField] private Image imgTimerFill;
    public Text timerText;                 // ✅ 把倒數顯示用的 Text 拖進來
    public float timeLimitSeconds = 60f;   // ✅ 60 秒
    public bool autoStartTimer = true;     // ✅ 是否自動啟動倒數
    public bool startTimerAfterInitFires = true; // ✅ 等火焰初始化後再開始（避免誤判）
    public bool showMinutesSeconds = true; // ✅ 顯示成 mm:ss (例如 01:00)

    [Header("Timer Messages")]
    public string failMessage = "失敗：時間到仍未成功滅火。";
    public bool overrideTextWhenFailed = true;

    // ===== 成功 / 失敗後的動作（拖進來）=====
    [Header("On Success / Fail (Drag actions here)")]
    public UnityEvent onSuccess; // ✅ 成功時會呼叫
    public UnityEvent onFail;    // ✅ 失敗時會呼叫

    // （可選）成功/失敗時自動顯示/隱藏某些物件（例如 Panel）
    [Header("Optional: Auto Toggle GameObjects")]
    public GameObject[] enableOnSuccess;
    public GameObject[] disableOnSuccess;
    public GameObject[] enableOnFail;
    public GameObject[] disableOnFail;

    // ===== 前三步：抓取即前進 =====
    [Header("三個『抓取即前進』的 XRGrabInteractable")]
    public XRGrabInteractable[] grabSteps = new XRGrabInteractable[3];

    // ===== 第四步：Trigger =====
    [Header("第四步：手把 Trigger（Input Action）")]
    public InputActionReference triggerAction;
    [Range(0f, 1f)] public float triggerThreshold = 0.25f;

    // ===== 四段文案 =====
    [Header("四段文字")]
    [TextArea] public string step1 = "步驟 1";
    [TextArea] public string step2 = "步驟 2";
    [TextArea] public string step3 = "步驟 3";
    [TextArea] public string step4 = "步驟 4（按 Trigger 後）";

    // ===== 行為選項 =====
    [Header("選項")]
    public bool allowSameObjectMultipleTimes = false; // 同一物件重複抓也能推進
    public bool resetWhenAllReleased = false;         // 抓到一半全部放開→重置

    // ===== 完成條件（全部火焰熄滅）=====
    [Header("完成條件：全部指定的火焰熄滅")]
    public Flammable[] watchFires;                     // 直接把 Flammable 拖進來
    public string completedMessage = "恭喜完成了！";
    public bool overrideTextWhenCompleted = true;      // 完成後是否覆蓋文字

    // --- 內部狀態 ---
    int stepIndex = -1; // -1=尚未開始；0..2=第1~3段；3=第4段
    readonly HashSet<XRGrabInteractable> counted = new HashSet<XRGrabInteractable>();
    readonly HashSet<XRGrabInteractable> currentlySelected = new HashSet<XRGrabInteractable>();

    int firesRemaining = 0;

    bool completedShown = false;
    bool failedShown = false;

    // timer
    float timeLeft = 0f;
    bool timerRunning = false;
    bool firesInitialized = false;

    void Awake()
    {
        if (!legacyText) legacyText = GetComponent<Text>();
        if (!legacyText) Debug.LogWarning("[StepTextByGrabsUI] 找不到 Text (Legacy)。");

        // timerText 不強制一定要有（你若不顯示可留空）
        timeLeft = timeLimitSeconds;
        UpdateTimerText();
    }

    void OnEnable()
    {
        // XR 事件訂閱
        foreach (var g in grabSteps)
        {
            if (!g) continue;
            g.selectEntered.AddListener(OnSelectEntered);
            g.selectExited.AddListener(OnSelectExited);
        }

        if (triggerAction != null)
        {
            if (!triggerAction.action.enabled) triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPerformed;
        }

        // 火焰事件：此時只綁事件，不計數（避免「一開始就完成」）
        if (watchFires != null)
        {
            foreach (var f in watchFires)
            {
                if (!f) continue;
                f.onIgnited.AddListener(OnOneFireIgnited);
                f.onExtinguished.AddListener(OnOneFireExtinguished);
            }
        }
    }

    void Start()
    {
        // 等一幀，讓 Flammable.Start() 先跑（可能在那邊 Ignite）
        StartCoroutine(InitFiresAfterStart());
    }

    IEnumerator InitFiresAfterStart()
    {
        yield return null; // 等 1 frame
        RecountFires();    // 這時再看 IsBurning 就不會誤判
        firesInitialized = true;

        // 若一開始就全熄滅（或根本沒火），直接算成功
        if (firesRemaining == 0 && watchFires != null && watchFires.Length > 0)
        {
            ShowCompleted();
            yield break;
        }

        // ✅ 自動開始倒數（預設）
        if (autoStartTimer)
        {
            if (!startTimerAfterInitFires || firesInitialized)
                StartTimer();
        }
    }

    void OnDisable()
    {
        foreach (var g in grabSteps)
        {
            if (!g) continue;
            g.selectEntered.RemoveListener(OnSelectEntered);
            g.selectExited.RemoveListener(OnSelectExited);
        }

        if (triggerAction != null)
        {
            triggerAction.action.performed -= OnTriggerPerformed;
        }

        if (watchFires != null)
        {
            foreach (var f in watchFires)
            {
                if (!f) continue;
                f.onIgnited.RemoveListener(OnOneFireIgnited);
                f.onExtinguished.RemoveListener(OnOneFireExtinguished);
            }
        }
    }

    void Update()
    {
        if (!timerRunning) return;
        if (completedShown || failedShown) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;
        UpdateTimerText();

        if (timeLeft <= 0f)
        {
            // 時間到：若仍未全熄滅 → 失敗
            if (firesRemaining > 0)
                ShowFailed();
            else
                ShowCompleted(); // 理論上不太會發生，但保險
        }
    }

    // ===== Timer API =====
    public void StartTimer()
    {
        if (completedShown || failedShown) return;

        timeLeft = timeLimitSeconds;
        timerRunning = true;
        UpdateTimerText();
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    void UpdateTimerText()
    {
        if (!timerText) return;

        if (!showMinutesSeconds)
        {
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();
            return;
        }

        int t = Mathf.CeilToInt(timeLeft);
        timerText.text = $"{t}";

        if (imgTimerFill != null && timeLimitSeconds > 0f)
        {
            imgTimerFill.fillAmount = Mathf.Clamp01(timeLeft / timeLimitSeconds);
            // 如果你想「時間越少越滿」就改成：
            // imgTimerFill.fillAmount = 1f - Mathf.Clamp01(timeLeft / totalTime);
        }
    }

    // ===== XR：抓取事件 =====
    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (completedShown || failedShown) return;

        var grab = args.interactableObject as XRGrabInteractable;
        if (!grab) return;

        currentlySelected.Add(grab);

        if (stepIndex < 2)
        {
            bool firstTime = counted.Add(grab);
            if (allowSameObjectMultipleTimes || firstTime)
            {
                stepIndex++;
                Apply(stepIndex);
            }
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (completedShown || failedShown) return;

        var grab = args.interactableObject as XRGrabInteractable;
        if (!grab) return;

        currentlySelected.Remove(grab);

        if (resetWhenAllReleased && currentlySelected.Count == 0 && stepIndex < 3)
        {
            ResetSteps();
        }
    }

    // ===== XR：Trigger（Input System）=====
    void OnTriggerPerformed(InputAction.CallbackContext ctx)
    {
        if (completedShown || failedShown) return;

        if (stepIndex != 2) return;               // 必須先到第3段
        if (currentlySelected.Count == 0) return; // 必須正在抓著任一物件

        float v = 1f;
        if (ctx.control?.valueType == typeof(float))
            v = ctx.ReadValue<float>();

        if (v >= triggerThreshold)
        {
            stepIndex = 3;
            Apply(stepIndex);
        }
    }

    // ===== 火焰事件處理（用重新計數，簡單穩定）=====
    void OnOneFireIgnited() { RecountFires(); }
    void OnOneFireExtinguished() { RecountFires(); }

    void RecountFires()
    {
        if (watchFires == null || watchFires.Length == 0)
        {
            firesRemaining = 0;
            return;
        }

        int now = 0;
        foreach (var f in watchFires)
            if (f && f.IsBurning) now++;

        firesRemaining = now;

        if (firesRemaining == 0)
            ShowCompleted();
    }

    void ShowCompleted()
    {
        if (completedShown || failedShown) return;
        completedShown = true;

        StopTimer();

        if (overrideTextWhenCompleted && legacyText)
            legacyText.text = completedMessage;

        // ✅ 執行成功事件（你可以拖方法進去）
        onSuccess?.Invoke();

        // ✅ 可選：自動顯示/隱藏物件
        ToggleObjects(enableOnSuccess, true);
        ToggleObjects(disableOnSuccess, false);
    }

    void ShowFailed()
    {
        if (failedShown || completedShown) return;
        failedShown = true;

        StopTimer();

        if (overrideTextWhenFailed && legacyText)
            legacyText.text = failMessage;

        // ✅ 執行失敗事件（你可以拖方法進去）
        onFail?.Invoke();

        // ✅ 可選：自動顯示/隱藏物件
        ToggleObjects(enableOnFail, true);
        ToggleObjects(disableOnFail, false);
    }

    void ToggleObjects(GameObject[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (!list[i]) continue;
            list[i].SetActive(active);
        }
    }

    // ===== 工具 =====
    public void ResetSteps()
    {
        stepIndex = -1;
        counted.Clear();
        if (legacyText && !completedShown && !failedShown) legacyText.text = "";
    }

    void Apply(int i)
    {
        if (!legacyText) return;
        if (completedShown || failedShown) return; // 成功/失敗後不再被步驟覆寫

        switch (i)
        {
            case 0: legacyText.text = step1; break;
            case 1: legacyText.text = step2; break;
            case 2: legacyText.text = step3; break;
            case 3: legacyText.text = step4; break;
        }
    }

    // ✅ 給 UI Button / XR Button 的 OnClick 呼叫
    public void StartTimerByButton()
    {
        if (completedShown || failedShown) return;

        // 避免重複按一直重置時間（若你想每次按都重置，就把這行拿掉）
        if (timerRunning) return;

        // 如果火焰還沒初始化完，就等到初始化完再開始（更穩）
        if (!firesInitialized)
        {
            StartCoroutine(CoStartTimerWhenReady());
            return;
        }

        StartTimer();
    }

    IEnumerator CoStartTimerWhenReady()
    {
        while (!firesInitialized) yield return null;
        if (completedShown || failedShown) yield break;
        if (timerRunning) yield break;
        StartTimer();
    }

}
