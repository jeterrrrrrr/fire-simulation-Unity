using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [TextArea]
    public string hintMessage;

    public void Interact()
    {
        Debug.Log($"[Interact] 點擊到 {name}");

        // 顯示提示（你之前已經有 UIManager）
        UIManager.Instance.ShowMessage(hintMessage);

        // 數量 +1
        UIManager.Instance.AddFound();

        // 物件消失
        gameObject.SetActive(false);
    }
}
