using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI messageText;  // 顯示警告 / 安全提示
    public TextMeshProUGUI countText;    // 顯示找到的數量

    private int foundCount = 0;

    void Awake()
    {
        Instance = this;
    }

    public void ShowMessage(string msg)
    {
        messageText.text = msg;

        // 自動隱藏提示（2秒）
        CancelInvoke();
        Invoke("ClearMessage", 2f);
    }

    void ClearMessage()
    {
        messageText.text = "";
    }

    public void AddFound()
    {
        foundCount++;
        countText.text = "已找到：" + foundCount;
    }
}
