using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExtinguisherController : MonoBehaviour
{
    public ParticleSystem smokeEffect; // 滅火器的煙霧特效
    public InputActionReference spray_trigger; // 扳機動作輸入
    private void Start()
    {
        spray_trigger.action.performed += triggerAction;
    }

    private void OnDestroy()
    {
        spray_trigger.action.performed -= triggerAction;
    }
    private void triggerAction(InputAction.CallbackContext callBack)
    {
        
        if (callBack.action.ReadValue<float>() > 0.1f)
        {
            if (!smokeEffect.isPlaying) 
                smokeEffect.Play(); // 按下時播放煙霧
            print("播放煙霧");
        }
        else
        {
            if (smokeEffect.isPlaying)
                smokeEffect.Stop(); // 鬆開時停止煙霧
            print("停止煙霧");
        }
    }

    void Update()
    {

    }
}