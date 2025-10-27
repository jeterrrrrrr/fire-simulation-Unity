using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class PinInsertedState : MonoBehaviour
{
    [Header("References")]
    public ExtinguisherController controller;   // 根物件控制器；可留空自動找
    public Transform extinguisherRoot;          // 整支滅火器 Root；可留空用 controller.transform
    public Rigidbody pinRB;                     // 自動抓
    public XRGrabInteractable pinGrab;          // 自動抓

    [Tooltip("Pin 自己(含子)的碰撞體；留空自動抓")]
    public Collider[] pinColliders;

    [Tooltip("Root 下(排除 Pin)的所有碰撞體；留空自動抓")]
    public Collider[] rootOtherColliders;

    [Header("Behavior")]
    public bool startInserted = true;           // 進場視為已插著

    readonly List<(Collider a, Collider b)> ignoredPairs = new();
    bool ignoreApplied;

    void Reset() { pinRB = GetComponent<Rigidbody>(); pinGrab = GetComponent<XRGrabInteractable>(); }

    void Awake()
    {
        if (!pinRB) pinRB = GetComponent<Rigidbody>();
        if (!pinGrab) pinGrab = GetComponent<XRGrabInteractable>();
        if (!controller) controller = GetComponentInParent<ExtinguisherController>();
        if (!extinguisherRoot && controller) extinguisherRoot = controller.transform;

        if (pinColliders == null || pinColliders.Length == 0)
            pinColliders = GetComponentsInChildren<Collider>(includeInactive: true);

        if ((rootOtherColliders == null || rootOtherColliders.Length == 0) && extinguisherRoot)
        {
            var all = extinguisherRoot.GetComponentsInChildren<Collider>(includeInactive: true);
            var pinSet = new HashSet<Collider>(pinColliders);
            rootOtherColliders = all.Where(c => !pinSet.Contains(c)).ToArray();
        }
    }

    void OnEnable()
    {
        // 抓到之後：立刻解除「插著」狀態（動態＋恢復碰撞），並通知控制器
        pinGrab.selectEntered.AddListener(_ =>
        {
            SetInserted(false);
            controller?.MarkPinRemoved();
        });

        // 放手：下一幀再把剛體設回動態，覆蓋 XRI 的還原值
        pinGrab.selectExited.AddListener(_ => StartCoroutine(EnsureDynamicNextFrame()));

        SetInserted(startInserted);
    }

    void OnDisable()
    {
        pinGrab.selectEntered.RemoveAllListeners();
        pinGrab.selectExited.RemoveAllListeners();
        RestoreCollisions();
    }

    public void SetInserted(bool inserted)
    {
        if (inserted)
        {
            if (pinRB) { pinRB.isKinematic = true; pinRB.useGravity = false; }
            ApplyIgnore();          // 忽略 Pin ? Root 其他碰撞
        }
        else
        {
            EnsureDynamic();        // 先確保動態
            RestoreCollisions();    // 再恢復碰撞
        }
    }

    void EnsureDynamic()
    {
        if (!pinRB) return;
        pinRB.isKinematic = false;
        pinRB.useGravity = true;
    }

    IEnumerator EnsureDynamicNextFrame()
    {
        // 讓 XRI 先完成它的還原，再下一幀我們把它設回動態
        yield return null;
        EnsureDynamic();
    }

    void ApplyIgnore()
    {
        if (ignoreApplied) return;
        foreach (var bodyCol in rootOtherColliders)
            foreach (var pinCol in pinColliders)
            {
                if (!bodyCol || !pinCol) continue;
                Physics.IgnoreCollision(bodyCol, pinCol, true);
                ignoredPairs.Add((bodyCol, pinCol));
            }
        ignoreApplied = true;
    }

    void RestoreCollisions()
    {
        if (!ignoreApplied) return;
        foreach (var pair in ignoredPairs)
            if (pair.a && pair.b) Physics.IgnoreCollision(pair.a, pair.b, false);
        ignoredPairs.Clear();
        ignoreApplied = false;
    }

    public void ForceReinsert() => SetInserted(true);
}
