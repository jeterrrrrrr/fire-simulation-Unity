using UnityEngine;
using UnityEngine.InputSystem;
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

    bool open;
    float lastToggleTime;

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
            Debug.Log("[Door] Toggle action 已停用");
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

        if (Time.time - lastToggleTime < minToggleInterval)
        {
            Debug.Log("[Door] 操作太快，忽略一次");
            return;
        }

        float dist = Vector3.Distance(player.position, transform.position);
        Debug.Log("[Door] 玩家距離門：" + dist.ToString("F2"));

        if (dist > interactDistance)
        {
            Debug.Log("[Door] 太遠，不能互動");
            return;
        }

        lastToggleTime = Time.time;

        if (open)
        {
            Debug.Log("[Door] 嘗試關門");
            StartCoroutine(Closing());
        }
        else
        {
            Debug.Log("[Door] 嘗試開門");
            StartCoroutine(Opening());
        }
    }

    IEnumerator Opening()
    {
        openandclose.Play(openState);
        open = true;
        Debug.Log("[Door] 動畫播放：Opening");
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Closing()
    {
        openandclose.Play(closeState);
        open = false;
        Debug.Log("[Door] 動畫播放：Closing");
        yield return new WaitForSeconds(0.5f);
    }
}
