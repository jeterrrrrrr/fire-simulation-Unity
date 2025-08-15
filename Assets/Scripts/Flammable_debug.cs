using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Reflection;

[ExecuteAlways]
[DisallowMultipleComponent]
public class FlammableDebug : MonoBehaviour
{
    public Flammable target;
    public bool drawWhenUnselected = false;
    public bool drawNeighbors = true;
    public Color radiusColor = new Color(1f, .5f, 0f, 0.15f);
    public Color radiusEdge = new Color(1f, .4f, 0f, 0.9f);
    public Color linkColor = new Color(1f, 0.8f, 0.2f, 0.9f);
    public float gizmoEdgeThickness = 2f;

    readonly List<Transform> _neighbors = new List<Transform>();
    float _lastScanTime;

    // �Ϯg�֨�
    FieldInfo fiHeat, fiIsBurning;

    void OnValidate()
    {
        if (!target) target = GetComponent<Flammable>();
        CacheReflection();
    }

    void CacheReflection()
    {
        if (target == null) return;
        var t = typeof(Flammable);
        fiHeat = fiHeat ?? t.GetField("heat", BindingFlags.Instance | BindingFlags.NonPublic);
        fiIsBurning = fiIsBurning ?? t.GetField("isBurning", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !drawWhenUnselected && !Selection.Contains(gameObject))
            return;
#endif
        if (!target) { target = GetComponent<Flammable>(); if (!target) return; }
        CacheReflection();

        var centerT = target.fireAnchor ? target.fireAnchor : target.transform;
        var center = centerT.position;

        // �C 0.2 ���@��
        if (Time.realtimeSinceStartup - _lastScanTime > 0.2f)
        {
            _neighbors.Clear();
            var mask = target.flammableMask;
            int found = 0;

            if (mask.value != 0 && target.spreadRadius > 0f)
            {
                var hits = Physics.OverlapSphere(center, target.spreadRadius, mask, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    var f = h.GetComponentInParent<Flammable>();
                    if (f != null && f != target)
                    {
                        var ft = f.fireAnchor ? f.fireAnchor : f.transform;
                        _neighbors.Add(ft);
                        found++;
                        float d = Vector3.Distance(center, ft.position);
                        Debug.Log($"[FlammableDebug] {name} �R���F�~: {f.name}, �Z��={d:F2}");
                    }
                }
            }

            // ����ثe����P���A
            float heat = fiHeat != null ? (float)fiHeat.GetValue(target) : -1f;
            bool isBurning = fiIsBurning != null ? (bool)fiIsBurning.GetValue(target) : false;
            bool canIgnite = heat >= target.ignitionHeat;
            string stateText = isBurning ? "�w�ۤ�" : "���ۤ�";

            Debug.Log(
                $"[FlammableDebug] {name} ���y���� | �F�~��={found} | ���q={heat:F1}/{target.ignitionHeat} (�F��? {canIgnite}) | Fuel={target.fuel:F1} | SpreadR={target.spreadRadius} | ���A={stateText}"
            );

            _lastScanTime = Time.realtimeSinceStartup;
        }
    }

    void OnDrawGizmos()
    {
        if (drawWhenUnselected) Draw();
    }

    void OnDrawGizmosSelected()
    {
        if (!drawWhenUnselected) Draw();
    }

    void Draw()
    {
        if (!target) { target = GetComponent<Flammable>(); if (!target) return; }
        CacheReflection();

        var centerT = target.fireAnchor ? target.fireAnchor : target.transform;
        var pos = centerT.position;
        float r = Mathf.Max(0f, target.spreadRadius);

        // �e�d��
        Gizmos.color = radiusColor;
        Gizmos.DrawSphere(pos, r);
#if UNITY_EDITOR
        Handles.color = radiusEdge;
        Handles.DrawWireDisc(pos, Vector3.up, r, gizmoEdgeThickness);
        Handles.DrawWireDisc(pos, Vector3.right, r, gizmoEdgeThickness * 0.5f);
        Handles.DrawWireDisc(pos, Vector3.forward, r, gizmoEdgeThickness * 0.5f);

        // ��r�]�t���A�P����^
        float heat = fiHeat != null ? (float)fiHeat.GetValue(target) : -1f;
        bool isBurning = fiIsBurning != null ? (bool)fiIsBurning.GetValue(target) : false;
        bool canIgnite = heat >= target.ignitionHeat;
        string stateText = isBurning ? "�w�ۤ�" : "���ۤ�";

        string label =
            $"Flammable\n" +
            $"Heat:{heat:F1}/{target.ignitionHeat} (�F��? {canIgnite})\n" +
            $"Fuel:{target.fuel:F1}\n" +
            $"SpreadR:{target.spreadRadius}\n" +
            $"Mask:{LayerMaskToString(target.flammableMask)}\n" +
            $"State:{stateText}";
        Handles.Label(pos + Vector3.up * (r + 0.1f), label);

        // �F�~�s�u
        if (drawNeighbors && _neighbors.Count > 0)
        {
            Handles.color = linkColor;
            foreach (var t in _neighbors)
                if (t) Handles.DrawLine(pos, t.position, gizmoEdgeThickness);
        }
#endif
    }

#if UNITY_EDITOR
    static string LayerMaskToString(LayerMask m)
    {
        if (m.value == 0) return "Nothing";
        var names = new List<string>();
        for (int i = 0; i < 32; i++) if (((1 << i) & m.value) != 0) names.Add(LayerMask.LayerToName(i));
        return string.Join("|", names);
    }
#endif
}
