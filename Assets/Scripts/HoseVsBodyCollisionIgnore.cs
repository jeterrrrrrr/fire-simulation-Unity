using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class HoseVsBodyCollisionIgnore : MonoBehaviour
{
    [Header("Assign")]
    public Transform extinguisherRoot;   // 整支滅火器 Root
    public Transform hoseRoot;           // 皮管所有段的父物件 (HoseRoot)
    [Tooltip("是否也忽略噴嘴與本體的碰撞")]
    public bool ignoreNozzle = false;
    [Tooltip("噴嘴名稱關鍵字，用於排除/包含")]
    public string nozzleNameContains = "Nozzle";

    readonly List<(Collider a, Collider b)> pairs = new();

    void OnEnable()
    {
        if (!extinguisherRoot || !hoseRoot) return;

        var bodyCols = extinguisherRoot.GetComponentsInChildren<Collider>(true);
        var hoseCols = hoseRoot.GetComponentsInChildren<Collider>(true)
                       .Where(c => ignoreNozzle || !c.transform.name.Contains(nozzleNameContains));

        foreach (var b in bodyCols)
            foreach (var h in hoseCols)
            {
                if (!b || !h || ReferenceEquals(b, h)) continue;
                Physics.IgnoreCollision(b, h, true);
                pairs.Add((b, h));
            }
    }

    void OnDisable()
    {
        foreach (var p in pairs)
            if (p.a && p.b) Physics.IgnoreCollision(p.a, p.b, false);
        pairs.Clear();
    }
}

