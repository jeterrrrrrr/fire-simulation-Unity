using UnityEngine;

public class PauseHelper : MonoBehaviour
{
    [Header("Is Pause When Start Secce?")]
    [SerializeField] private bool isPsuseStart = false;

    void Awake()
    {
        if (isPsuseStart) {
            Pause();
        }
        
    }
    public void Pause()
    {
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }
    public void Resume()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
    

    public void SetPause(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
    }
}
