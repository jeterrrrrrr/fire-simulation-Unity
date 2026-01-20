using UnityEngine;

public class GameStateEventTarget : MonoBehaviour
{
    [Header("對應關卡：1~4")]
    [Range(1, 4)]
    public int levelNumber = 1;

    // ✅ 給 UnityEvent(onSuccess) 拖拉用
    public void MarkCleared()
    {
        GameState.SetCleared(levelNumber);
        Debug.Log($"[GameStateEventTarget] Level {levelNumber} -> Cleared");
    }

    // ✅ 給 UnityEvent(onFail) 拖拉用（你要失敗時不改狀態也可不綁）
    public void MarkNotCleared()
    {
        GameState.SetNotCleared(levelNumber);
        Debug.Log($"[GameStateEventTarget] Level {levelNumber} -> Not Cleared");
    }
}
