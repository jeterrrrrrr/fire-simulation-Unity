using UnityEngine;
using UnityEngine.UI;

public class FlammableIgnitionHeatButton : MonoBehaviour
{
    [Header("拖進來")]
    public Flammable flammable;   // 目標可燃物
    public Button button;         // Legacy UI Button

    [Header("按下按鈕要設定的 ignitionHeat")]
    public float ignitionHeatToSet = 100f;

    void Reset()
    {
        // 如果此腳本掛在 Button 上，會自動抓到
        if (!button) button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (button) button.onClick.AddListener(OnClick);
    }

    void OnDisable()
    {
        if (button) button.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        if (!flammable)
        {
            Debug.LogWarning("[FlammableIgnitionHeatButton] flammable 沒有指定", this);
            return;
        }

        flammable.ignitionHeat = ignitionHeatToSet;
        flammable.Ignite();
        Debug.Log($"[Set ignitionHeat] {flammable.name} => {ignitionHeatToSet}", this);
    }

    // （可選）如果你想在 Button 的 OnClick 直接傳入數字，也可以用這個
    public void SetIgnitionHeat(float value)
    {
        if (!flammable) return;
        flammable.ignitionHeat = value;
        Debug.Log($"[Set ignitionHeat] {flammable.name} => {value}", this);
    }
}
