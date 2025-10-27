using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ExtinguisherController : MonoBehaviour
{
    [Header("FX")]
    public ParticleSystem smokeEffect;          // �Q�g�ɤl�]���b�Q�f�^
    public AudioSource sprayLoop;               // �i��G�Q�g�n

    [Header("Input")]
    public InputActionReference spray_trigger;  // �j�u��� Trigger / Activate�v
    [Range(0f, 1f)] public float triggerThreshold = 0.25f;

    [Header("State (�ѥ~���ƥ�])")]
    public bool isPinRemoved = false;           // Pin �}����_�ɩI�s MarkPinRemoved()
    public bool isHoseDetached = false;         // HoseSocket �ޥX�ɩI�s MarkHoseDetached()

    [Header("XR")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable xrGrab;           // ������ʡ]��_��~��Q�^

    // ����
    bool isSpraying = false;
    float triggerValue = 0f;
    bool isHeld = false;                        // �O�_�Q XR �����

    void OnEnable()
    {
        if (spray_trigger != null)
        {
            // �T�O�iŪ�ȡF�]�q�\ canceled �H�K�ʧ@�Q���ήɯఱ�Q
            spray_trigger.action.Enable();
            spray_trigger.action.canceled += OnTriggerCanceled;
        }

        if (xrGrab == null) xrGrab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (xrGrab != null)
        {
            xrGrab.selectEntered.AddListener(OnGrabbed);
            xrGrab.selectExited.AddListener(OnReleased);
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

        if (xrGrab != null)
        {
            xrGrab.selectEntered.RemoveListener(OnGrabbed);
            xrGrab.selectExited.RemoveListener(OnReleased);
        }
    }

    void Update()
    {
        // �v�VŪ������ȡ]��u�a performed �i�a�^
        triggerValue = (spray_trigger != null) ? spray_trigger.action.ReadValue<float>() : 0f;

        // �u���Q XR ����ɤ~���\�Q��
        bool wantSpray = isHeld && isPinRemoved && isHoseDetached && triggerValue >= triggerThreshold;

        if (wantSpray && !isSpraying) StartSpray();
        else if (!wantSpray && isSpraying) StopSpray();
    }

    // �� Pin/Nozzle �ƥ�I�s
    public void MarkPinRemoved()
    {
        if (isPinRemoved) return;
        isPinRemoved = true;
        Debug.Log("[Extinguisher] Pin removed.");
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

    // XR �ƥ�G��_/��U
    void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        if (isSpraying) StopSpray();
    }
}
