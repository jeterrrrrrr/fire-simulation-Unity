using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils; // 若你使用 XR Origin 需要這個
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;



public class TurnModeSwitcher : MonoBehaviour
{
    public ContinuousTurnProvider continuousTurn;
    public SnapTurnProvider snapTurn;
    
    public Toggle smoothToggle;
    public Toggle snapToggle;

    void Start()
    {
        smoothToggle.onValueChanged.AddListener(OnSmoothChanged);
        snapToggle.onValueChanged.AddListener(OnSnapChanged);
    }

    void OnSmoothChanged(bool isOn)
    {
        if (isOn)
        {
            continuousTurn.enabled = true;
            snapTurn.enabled = false;
            snapToggle.isOn = false;
        }
    }

    void OnSnapChanged(bool isOn)
    {
        if (isOn)
        {
            snapTurn.enabled = true;
            continuousTurn.enabled = false;
            smoothToggle.isOn = false;
        }
    }
}
