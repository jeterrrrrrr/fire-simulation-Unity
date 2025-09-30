using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PinInsertedState : MonoBehaviour
{
    public ExtinguisherController controller;
    XRGrabInteractable grab; Rigidbody rb;
    Collider[] pinCols; Collider[] bodyCols; readonly List<(Collider, Collider)> pairs = new();

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        pinCols = GetComponentsInChildren<Collider>(true);

        // 收集「瓶身」的碰撞體（把 Pin 自己排除）
        var all = GetComponentsInParent<Collider>(true);
        var set = new HashSet<Collider>(pinCols);
        var list = new List<Collider>();
        foreach (var c in all) if (!set.Contains(c)) list.Add(c);
        bodyCols = list.ToArray();

        SetInserted(true);
        grab.selectEntered.AddListener(_ => { SetInserted(false); controller?.MarkPinRemoved(); });
    }

    void SetInserted(bool inserted)
    {
        if (rb) rb.isKinematic = inserted;
        foreach (var b in bodyCols) foreach (var p in pinCols)
            {
                Physics.IgnoreCollision(b, p, inserted);
                if (inserted) pairs.Add((b, p));
            }
        if (!inserted) pairs.Clear();
    }
}
