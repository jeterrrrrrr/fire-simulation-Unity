using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI (Legacy)")]
    public Slider sliderVolume; // 0~1

    [Header("PlayerPrefs")]
    public string keyVolume = "SET_VOL";

    [Header("Default (首次進入)")]
    [Range(0f, 1f)] public float defaultValue = 0.5f;

    void Start()
    {
        float vol = PlayerPrefs.GetFloat(keyVolume, defaultValue);

        if (sliderVolume != null)
        {
            sliderVolume.SetValueWithoutNotify(vol);
            sliderVolume.onValueChanged.AddListener(OnVolumeChanged);
        }

        ApplyVolume(vol);
    }

    void OnDestroy()
    {
        if (sliderVolume != null)
            sliderVolume.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    void OnVolumeChanged(float v)
    {
        v = Mathf.Clamp01(v);
        ApplyVolume(v);

        PlayerPrefs.SetFloat(keyVolume, v);
        PlayerPrefs.Save();
    }

    void ApplyVolume(float v)
    {
        // 最簡單：全域音量（影響所有 AudioSource）
        AudioListener.volume = v;
    }
}
