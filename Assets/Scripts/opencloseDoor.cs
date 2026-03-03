using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class opencloseDoorVR : MonoBehaviour
{
    [Header("Animation")]
    public Animator openandclose;      // 指到門上的 Animator
    public string openState = "Opening";
    public string closeState = "Closing";

    [Header("Interaction")]
    public Transform player;           // 建議拖 Main Camera
    public float interactDistance = 2f;
    public InputActionReference toggleAction; // 綁 XRI 按鍵(Trigger/Primary等)

    [Header("Options")]
    public float minToggleInterval = 0.35f;   // 防止連點
    public bool startOpened = false;

    [Header("Events")]
    [Tooltip("門「開啟完成」時觸發（可拖事件）")]
    public UnityEvent onOpened;
    [Tooltip("門「關閉完成」時觸發（可拖事件）")]
    public UnityEvent onClosed;

    bool open;
    float lastToggleTime;
    bool isBusy = false; // 動畫進行中，避免重複開關

    void Awake()
    {
        open = startOpened;
    }

    void OnEnable()
    {
        if (toggleAction != null)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += OnTogglePerformed;
            Debug.Log("[Door] Toggle action 已啟用");
        }
    }

    void OnDisable()
    {
        if (toggleAction != null)
        {
            toggleAction.action.performed -= OnTogglePerformed;
            toggleAction.action.Disable();
            //Debug.Log("[Door] Toggle action 已停用");
        }
    }

    void OnTogglePerformed(InputAction.CallbackContext _)
    {
        Debug.Log("[Door] 偵測到按鍵輸入");
        TryToggle();
    }

    void TryToggle()
    {
        if (!openandclose) { Debug.LogWarning("[Door] 缺少 Animator！"); return; }
        if (!player) { Debug.LogWarning("[Door] 缺少 Player Transform！"); return; }
        if (isBusy) return;

        if (Time.time - lastToggleTime < minToggleInterval) return;

        float dist = Vector3.Distance(player.position, transform.position);
        if (dist > interactDistance) return;

        lastToggleTime = Time.time;

        if (open) StartCoroutine(Closing());
        else StartCoroutine(Opening());
    }

    IEnumerator Opening()
    {
        isBusy = true;

        openandclose.Play(openState);
        open = true;

        // 你原本是固定等 0.5s；我保留
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[Door] Opened");
        onOpened?.Invoke();

        isBusy = false;
    }

    IEnumerator Closing()
    {
        isBusy = true;

        openandclose.Play(closeState);
        open = false;

        yield return new WaitForSeconds(0.5f);

        Debug.Log("[Door] Closed");
        onClosed?.Invoke();

        isBusy = false;
    }
}
