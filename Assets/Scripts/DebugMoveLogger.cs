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
        // �p��o�@�V����m�t
        Vector3 delta = transform.position - lastPosition;

        // Unity �� CharacterController �S�����} "Move()" �O�_�Q�I�s�A
        // ���u�n���b�o�V���ʨåBĲ�o�F Collision�A�N�N�� Move() �ѻP�䤤
        if (delta.magnitude > 0.0001f)
        {
            Debug.Log($"[CharacterController] ���ʶq = {delta} | isGrounded = {cc.isGrounded}");
        }

        lastPosition = transform.position;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log($"[CharacterController] �I���� {hit.gameObject.name}");
    }
}

