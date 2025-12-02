using UnityEngine;

/// <summary>
/// 把所有「會讓玩家移動 / 轉向 / 傳送」的元件都丟進來，
/// 需要鎖住時就整批 enabled = false。
/// </summary>
public class PlayerMovementLock : MonoBehaviour
{
    [Header("要一起鎖定的元件（移動、轉向、傳送等）")]
    public MonoBehaviour[] componentsToDisable;

    [Header("進場時是否自動鎖定")]
    public bool lockOnStart = false;

    void Start()
    {
        if (lockOnStart)
        {
            SetMovementEnabled(false);
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (componentsToDisable == null) return;

        foreach (var comp in componentsToDisable)
        {
            if (comp == null) continue;
            comp.enabled = enabled;
        }
    }

    public void Lock()
    {
        SetMovementEnabled(false);
    }

    public void Unlock()
    {
        SetMovementEnabled(true);
    }
}
