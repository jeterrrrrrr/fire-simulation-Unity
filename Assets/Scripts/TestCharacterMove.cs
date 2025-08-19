using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestCharacterMove : MonoBehaviour
{
    public float speed = 2.0f;   // 移動速度
    public float gravity = -9.81f;
    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 用鍵盤方向鍵 / WASD 移動 (僅測試)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // 用 CharacterController.Move 來推動
        controller.Move(move * speed * Time.deltaTime);

        // 簡單模擬重力
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
