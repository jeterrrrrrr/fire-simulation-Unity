using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExtinguisherController : MonoBehaviour
{
    public ParticleSystem smokeEffect; // �������������S��
    public InputActionReference spray_trigger; // ����ʧ@��J
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
                smokeEffect.Play(); // ���U�ɼ������
            print("�������");
        }
        else
        {
            if (smokeEffect.isPlaying)
                smokeEffect.Stop(); // �P�}�ɰ������
            print("�������");
        }
    }

    void Update()
    {

    }
}