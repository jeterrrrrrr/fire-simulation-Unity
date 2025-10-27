using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(ParentConstraint))]
public class NozzleDock : MonoBehaviour
{
    public XRGrabInteractable grab;
    ParentConstraint pc;
    void Awake() { pc = GetComponent<ParentConstraint>(); if (!grab) grab = GetComponent<XRGrabInteractable>(); }
    void OnEnable()
    {
        grab.selectEntered.AddListener(_ => pc.weight = 0f);
        grab.selectExited.AddListener(_ => pc.weight = 1f);
    }
    void OnDisable()
    {
        grab.selectEntered.RemoveAllListeners();
        grab.selectExited.RemoveAllListeners();
    }
}
