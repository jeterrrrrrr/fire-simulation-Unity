using UnityEngine;

public class UIABStateController : MonoBehaviour
{
    public enum UIState { ShowA = 0, ShowB = 1, HideAll = 2 }

    [Header("把場景內的兩個 Panel/Canvas 拖進來")]
    public GameObject uiA;
    public GameObject uiB;

    [Header("是否要重開遊戲也記住狀態（用 PlayerPrefs）")]
    public bool persistAcrossRestart = false;

    private const string KEY = "UIAB_STATE";
    private static bool stateInited = false;
    private static UIState state = UIState.ShowA; // 預設第一次進來顯示 A

    void Awake()
    {
        InitStateIfNeeded();
        Apply();
    }

    void InitStateIfNeeded()
    {
        if (stateInited) return;
        stateInited = true;

        if (persistAcrossRestart)
        {
            state = (UIState)PlayerPrefs.GetInt(KEY, (int)UIState.ShowA);
        }
        else
        {
            state = UIState.ShowA; // 本次執行預設顯示 A
        }
    }

    void SaveState()
    {
        if (!persistAcrossRestart) return;
        PlayerPrefs.SetInt(KEY, (int)state);
        PlayerPrefs.Save();
    }

    void Apply()
    {
        if (uiA) uiA.SetActive(state == UIState.ShowA);
        if (uiB) uiB.SetActive(state == UIState.ShowB);
        if (state == UIState.HideAll)
        {
            if (uiA) uiA.SetActive(false);
            if (uiB) uiB.SetActive(false);
        }
    }

    // ✅ 你要的：按某按鈕後，A 預設隱藏、B 預設顯示（之後切回此 Scene 也會維持）
    public void SetDefaultToB()
    {
        state = UIState.ShowB;
        SaveState();
        Apply();
    }

    // 如果你想要回到 A（例如 Debug 或重新教學）
    public void SetDefaultToA()
    {
        state = UIState.ShowA;
        SaveState();
        Apply();
    }

    // 如果 B 看完後想之後都不再顯示（同一次執行或永久，依 persistAcrossRestart）
    public void HideAll()
    {
        state = UIState.HideAll;
        SaveState();
        Apply();
    }
}
