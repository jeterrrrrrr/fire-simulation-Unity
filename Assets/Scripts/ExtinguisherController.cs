using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ExtinguisherController : MonoBehaviour
{
    [Header("FX")]
    public ParticleSystem smokeEffect;          // 噴射粒子（掛在噴口）
    public AudioSource sprayLoop;               // 可選：噴射聲

    [Header("Input")]
    public InputActionReference spray_trigger;  // 綁「手把 Trigger / Activate」
    [Range(0f, 1f)] public float triggerThreshold = 0.25f;

    [Header("State")]
    public bool isPinRemoved = false;           // Pin 腳本抓起時呼叫 MarkPinRemoved()
    public bool isHoseDetached = false;         // HoseSocket 拔出時呼叫 MarkHoseDetached()

    [Header("XR - Grabs")]
    public XRGrabInteractable[] xrGrabs;

    [Header("XR - Pin")]
    public XRGrabInteractable pinGrab;          // ← 在 Inspector 把「Pin物件」的 XRGrabInteractable 拖進來
    public bool onlyHandGrabRemovesPin = false; // 若勾選，僅限 XRDirectInteractor（手）抓到才算拔出

    // 內部
    bool isSpraying = false;
    float triggerValue = 0f;
    bool isHeld = false;            // 是否被任一 XR 抓取
    int heldCount = 0;

    void OnEnable()
    {
        if (spray_trigger != null)
        {
            spray_trigger.action.Enable();
            spray_trigger.action.canceled += OnTriggerCanceled;
        }

        if (xrGrabs == null || xrGrabs.Length == 0)
            xrGrabs = GetComponentsInChildren<XRGrabInteractable>(true);

        foreach (var grab in xrGrabs)
        {
            if (grab == null) continue;
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }

        // 監聽 Pin 的抓取事件（抓到就把 isPinRemoved = true）
        if (pinGrab != null)
        {
            pinGrab.selectEntered.AddListener(OnPinGrabbed);
        }

        StopFXImmediate();
    }

    void OnDisable()
    {
        if (spray_trigger != null)
        {
            spray_trigger.action.canceled -= OnTriggerCanceled;
            spray_trigger.action.Disable();
        }

        foreach (var grab in xrGrabs)
        {
            if (grab == null) continue;
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }

        if (pinGrab != null)
        {
            pinGrab.selectEntered.RemoveListener(OnPinGrabbed);
        }
    }

    void Update()
    {
        triggerValue = (spray_trigger != null) ? spray_trigger.action.ReadValue<float>() : 0f;

        bool wantSpray = isHeld && isPinRemoved && isHoseDetached && triggerValue >= triggerThreshold;

        if (wantSpray && !isSpraying) StartSpray();
        else if (!wantSpray && isSpraying) StopSpray();
    }

    // 供 Pin/Nozzle 事件呼叫（保留原API）
    public void MarkPinRemoved()
    {
        if (isPinRemoved) return;
        isPinRemoved = true;
        Debug.Log("[Extinguisher] Pin removed (manual call).");
    }

    public void MarkHoseDetached()
    {
        if (isHoseDetached) return;
        isHoseDetached = true;
        Debug.Log("[Extinguisher] Hose detached.");
    }

    void OnTriggerCanceled(InputAction.CallbackContext _)
    {
        if (isSpraying) StopSpray();
    }

    void StartSpray()
    {
        isSpraying = true;
        if (smokeEffect && !smokeEffect.isPlaying) smokeEffect.Play();
        if (sprayLoop && !sprayLoop.isPlaying) sprayLoop.Play();
        Debug.Log("[Extinguisher] START spraying.");
    }

    void StopSpray()
    {
        isSpraying = false;
        if (smokeEffect && smokeEffect.isPlaying)
            smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (sprayLoop && sprayLoop.isPlaying) sprayLoop.Stop();
        Debug.Log("[Extinguisher] STOP spraying.");
    }

    void StopFXImmediate()
    {
        if (smokeEffect)
            smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sprayLoop) sprayLoop.Stop();
        isSpraying = false;
    }

    // XR 事件：抓起/放下（任一 Grab 被抓取就算持有）
    void OnGrabbed(SelectEnterEventArgs args)
    {
        heldCount = Mathf.Max(0, heldCount + 1);
        isHeld = heldCount > 0;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        heldCount = Mathf.Max(0, heldCount - 1);
        isHeld = heldCount > 0;
        if (!isHeld && isSpraying) StopSpray();
    }

    // XR 事件：Pin 被抓取 → 視為已拔
    void OnPinGrabbed(SelectEnterEventArgs args)
    {
        if (onlyHandGrabRemovesPin)
        {
            // 僅接受 XRDirectInteractor（手）抓取才算拔出
            if (!(args.interactorObject is XRDirectInteractor))
                return;
        }

        if (!isPinRemoved)
        {
            isPinRemoved = true;
            Debug.Log("[Extinguisher] Pin removed (grabbed).");
        }
    }
}
