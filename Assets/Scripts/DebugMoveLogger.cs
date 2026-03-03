using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DebugMoveLogger : MonoBehaviour
{
    private CharacterController cc;
    private Vector3 lastPosition;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        // 計算這一幀的位置差
        Vector3 delta = transform.position - lastPosition;

        // Unity 的 CharacterController 沒有公開 "Move()" 是否被呼叫，
        // 但只要它在這幀移動並且觸發了 Collision，就代表 Move() 參與其中
        if (delta.magnitude > 0.0001f)
        {
            Debug.Log($"[CharacterController] 移動量 = {delta} | isGrounded = {cc.isGrounded}");
        }

        lastPosition = transform.position;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log($"[CharacterController] 碰撞到 {hit.gameObject.name}");
    }
}

