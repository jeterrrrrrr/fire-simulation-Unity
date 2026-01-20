using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VolumeCountrol : MonoBehaviour
{
    [SerializeField] private Slider spatialAudioSlider;

    // 所有空間音效的來源
    private List<AudioSource> spatialAudioSources = new List<AudioSource>();

    private void Start()
    {
        // 找出場景中所有 AudioSource
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource source in allSources)
        {
            if (source.spatialBlend > 0f) // 只抓有啟用空間音效的
            {
                spatialAudioSources.Add(source);
            }
        }

        // 綁定事件
        spatialAudioSlider.onValueChanged.AddListener(SetSpatialAudioVolume);
    }

    private void SetSpatialAudioVolume(float value)
    {
        foreach (AudioSource source in spatialAudioSources)
        {
            source.volume = value;
        }
    }
    
}