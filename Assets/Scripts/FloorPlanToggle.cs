using UnityEngine;
using UnityEngine.InputSystem;

public class FloorMapHoldController : MonoBehaviour
{
    [Header("Assign your floor map Canvas or parent UI object")]
    public GameObject floorMapUI;

    [Header("Right-hand button")]
    public InputActionProperty rightHandPrimaryButton;

    private void Start()
    {
        // 遊戲開始時預設隱藏
        if (floorMapUI != null)
            floorMapUI.SetActive(false);
    }

    private void Update()
    {
        // 偵測「這個 Frame 是否剛按下按鈕」（只觸發一次）
        if (rightHandPrimaryButton.action.WasPressedThisFrame())
        {
            // 讀取目前狀態：如果是開的就變關，關的就變開
            bool currentState = floorMapUI.activeSelf;
            floorMapUI.SetActive(!currentState);
        }
    }
}