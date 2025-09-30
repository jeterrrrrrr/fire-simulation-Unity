using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public class ExtinguisherAutoZones : MonoBehaviour
{
    [Header("Main Extinguisher")]
    public XRGrabInteractable extinguisherGrab;
    
    public ParticleSystem sprayFX;
    public AudioSource sprayLoop;

    [Header("Spray Logic")]
    public bool isPinRemoved = false;
    public bool isHoseDetached = false;
    private bool isSpraying = false;

    [Header("Auto-Created Zones (Local space)")]
    public Vector3 pinZoneOffset = new Vector3(0.0f, 0.18f, 0.08f);
    public Vector3 pinZoneSize = new Vector3(0.05f, 0.05f, 0.05f);

    public Vector3 hoseZoneOffset = new Vector3(0.1f, 0.05f, 0.0f);
    public Vector3 hoseZoneSize = new Vector3(0.08f, 0.06f, 0.08f);

    [Header("Events (Optional)")]
    public UnityEvent OnPinRemoved;
    public UnityEvent OnHoseDetached;
    public UnityEvent OnStartSpray;
    public UnityEvent OnStopSpray;

    // runtime-created
    private GameObject pinZoneGO;
    private GameObject hoseZoneGO;

    void Reset()
    {
        // 嘗試自動找同物件上的 XRGrabInteractable
        if (!extinguisherGrab) extinguisherGrab = GetComponent<XRGrabInteractable>();
    }

    void Awake()
    {
        if (!extinguisherGrab)
        {
            extinguisherGrab = gameObject.AddComponent<XRGrabInteractable>();
        }

        // 粒子/聲音初始關閉
        if (sprayFX) sprayFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sprayLoop) sprayLoop.loop = true;

        // 建 PinZone
        pinZoneGO = CreateZoneChild("PinZone", pinZoneOffset, pinZoneSize, OnPinZoneSelected);

        // 建 HoseZone
        hoseZoneGO = CreateZoneChild("HoseZone", hoseZoneOffset, hoseZoneSize, OnHoseZoneSelected);

        // 綁定「扣扳機」事件（Activate/Deactivate）
        extinguisherGrab.activated.AddListener(OnActivate);
        extinguisherGrab.deactivated.AddListener(OnDeactivate);
    }

    private GameObject CreateZoneChild(string name, Vector3 localOffset, Vector3 localSize, System.Action selectAction)
    {
        var go = new GameObject(name);
        go.transform.SetParent(this.transform, worldPositionStays: false);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // 可見 Gizmo 幫你校準（Play 外）
        var gizmo = go.AddComponent<_ZoneGizmo>();

        // 加一個 BoxCollider + isTrigger
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = localSize;

        // 用 SimpleInteractable 讓手柄/射線可以「選取」
        var simple = go.AddComponent<XRSimpleInteractable>();
        // 只要被 select（抓/點）一次就當作完成該步驟
        simple.selectEntered.AddListener((_) =>
        {
            selectAction?.Invoke();
        });

        // 如果你用 Interaction Layer 管控手把/射線，這邊也可設定：
        // simple.interactionLayers = InteractionLayerMask.GetMask("Default");

        return go;
    }

    private void OnPinZoneSelected()
    {
        if (isPinRemoved) return;
        isPinRemoved = true;
        OnPinRemoved?.Invoke();
        Debug.Log("[Extinguisher] Pin removed via PinZone.");

        // 可選：把 PinZone 關掉，避免重複觸發
        if (pinZoneGO) pinZoneGO.SetActive(false);
    }

    private void OnHoseZoneSelected()
    {
        if (isHoseDetached) return;
        isHoseDetached = true;
        OnHoseDetached?.Invoke();
        Debug.Log("[Extinguisher] Hose detached via HoseZone.");

        if (hoseZoneGO) hoseZoneGO.SetActive(false);
    }

    private void OnActivate(ActivateEventArgs args)
    {
        // 只有在 Pin + Hose 都完成時，Activate 才開始噴
        if (!isSpraying && isPinRemoved && isHoseDetached)
        {
            StartSpray();
        }
    }

    private void OnDeactivate(DeactivateEventArgs args)
    {
        if (isSpraying)
        {
            StopSpray();
        }
    }

    private void StartSpray()
    {
        isSpraying = true;
        if (sprayFX && !sprayFX.isPlaying) sprayFX.Play();
        if (sprayLoop && !sprayLoop.isPlaying) sprayLoop.Play();
        OnStartSpray?.Invoke();
        Debug.Log("[Extinguisher] START spraying.");
    }

    private void StopSpray()
    {
        isSpraying = false;
        if (sprayFX && sprayFX.isPlaying) sprayFX.Stop();
        if (sprayLoop && sprayLoop.isPlaying) sprayLoop.Stop();
        OnStopSpray?.Invoke();
        Debug.Log("[Extinguisher] STOP spraying.");
    }

    // 只在編輯器裡幫忙畫框，方便你調整 offset/size
    private class _ZoneGizmo : MonoBehaviour
    {
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (!box) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0, 1, 0, 0.15f);
            Gizmos.DrawCube(Vector3.zero, box.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, box.size);
        }
#endif
    }
}
