using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class opencloseDoorVR : MonoBehaviour
{
    [Header("Animation")]
    public Animator openandclose;      // ������W�� Animator
    public string openState = "Opening";
    public string closeState = "Closing";

    [Header("Interaction")]
    public Transform player;           // ��ĳ�� Main Camera
    public float interactDistance = 2f;
    public InputActionReference toggleAction; // �j XRI ����(Trigger/Primary��)

    [Header("Options")]
    public float minToggleInterval = 0.35f;   // ����s�I
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
            Debug.Log("[Door] Toggle action �w�ҥ�");
        }
    }

    void OnDisable()
    {
        if (toggleAction != null)
        {
            toggleAction.action.performed -= OnTogglePerformed;
            toggleAction.action.Disable();
            Debug.Log("[Door] Toggle action �w����");
        }
    }

    void OnTogglePerformed(InputAction.CallbackContext _)
    {
        Debug.Log("[Door] ����������J");
        TryToggle();
    }

    void TryToggle()
    {
        if (!openandclose) { Debug.LogWarning("[Door] �ʤ� Animator�I"); return; }
        if (!player) { Debug.LogWarning("[Door] �ʤ� Player Transform�I"); return; }

        if (Time.time - lastToggleTime < minToggleInterval)
        {
            Debug.Log("[Door] �ާ@�ӧ֡A�����@��");
            return;
        }

        float dist = Vector3.Distance(player.position, transform.position);
        Debug.Log("[Door] ���a�Z�����G" + dist.ToString("F2"));

        if (dist > interactDistance)
        {
            Debug.Log("[Door] �ӻ��A���ब��");
            return;
        }

        lastToggleTime = Time.time;

        if (open)
        {
            Debug.Log("[Door] ��������");
            StartCoroutine(Closing());
        }
        else
        {
            Debug.Log("[Door] ���ն}��");
            StartCoroutine(Opening());
        }
    }

    IEnumerator Opening()
    {
        openandclose.Play(openState);
        open = true;
        Debug.Log("[Door] �ʵe����GOpening");
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Closing()
    {
        openandclose.Play(closeState);
        open = false;
        Debug.Log("[Door] �ʵe����GClosing");
        yield return new WaitForSeconds(0.5f);
    }
}
