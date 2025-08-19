using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestCharacterMove : MonoBehaviour
{
    public float speed = 2.0f;   // ���ʳt��
    public float gravity = -9.81f;
    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // ����L��V�� / WASD ���� (�ȴ���)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // �� CharacterController.Move �ӱ���
        controller.Move(move * speed * Time.deltaTime);

        // ²��������O
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
