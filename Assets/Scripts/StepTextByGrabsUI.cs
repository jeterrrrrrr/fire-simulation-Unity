using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;       // 新輸入系統
using UnityEngine.UI;                // Text (Legacy)
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StepTextByGrabsUI : MonoBehaviour
{
    // ===== 文字目標 =====
    [Header("Target Text (Legacy)")]
    public Text legacyText; // 可留空→自動抓本物件上的 Text

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

    void Awake()
    {
        if (!legacyText) legacyText = GetComponent<Text>();
        if (!legacyText) Debug.LogWarning("[StepTextByGrabsUI] 找不到 Text (Legacy)。");
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
        if (firesRemaining == 0 && watchFires != null && watchFires.Length > 0)
            ShowCompleted();
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
            // 若你想釋放可再關掉
            // triggerAction.action.Disable();
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

    // ===== XR：抓取事件 =====
    void OnSelectEntered(SelectEnterEventArgs args)
    {
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
        if (stepIndex != 2) return;              // 必須先到第3段
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
        if (completedShown) return;
        completedShown = true;

        if (overrideTextWhenCompleted && legacyText)
            legacyText.text = completedMessage;
    }

    // ===== 工具 =====
    public void ResetSteps()
    {
        stepIndex = -1;
        counted.Clear();
        if (legacyText && !completedShown) legacyText.text = "";
    }

    void Apply(int i)
    {
        if (!legacyText) return;
        if (completedShown) return; // 完成後不再被步驟覆寫

        switch (i)
        {
            case 0: legacyText.text = step1; break;
            case 1: legacyText.text = step2; break;
            case 2: legacyText.text = step3; break;
            case 3: legacyText.text = step4; break;
        }
    }
}
