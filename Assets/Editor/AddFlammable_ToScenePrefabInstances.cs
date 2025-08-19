#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class AddFlammable_ToScenePrefabInstances
{
    // 你的固定設定
    const string FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab";
    const string FIRE_LAYER_NAME = "FireLayer";

    [MenuItem("Tools/Fire/Add Flammable → Scene Prefab Instances (Hierarchy)")]
    public static void AddToSceneInstances()
    {
        // 載入 Fire Prefab
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FIRE_PREFAB_PATH);
        if (firePrefab == null)
        {
            EditorUtility.DisplayDialog("錯誤", $"找不到 Fire Prefab：\n{FIRE_PREFAB_PATH}", "OK");
            return;
        }

        // 計算 FireLayer 的 LayerMask
        int fireMask = LayerMask.GetMask(FIRE_LAYER_NAME);
        if (fireMask == 0)
        {
            bool cont = EditorUtility.DisplayDialog(
                "提醒",
                $"找不到圖層「{FIRE_LAYER_NAME}」。\n請先到 Project Settings → Tags and Layers 新增。\n\n仍要繼續（flammableMask 會設為 0）？",
                "仍然執行", "取消");
            if (!cont) return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("錯誤", "目前沒有開啟中的 Scene。", "OK");
            return;
        }

        var roots = scene.GetRootGameObjects();
        int touched = 0, skippedNonPrefab = 0;

        try
        {
            Undo.IncrementCurrentGroup();
            int opGroup = Undo.GetCurrentGroup();

            EditorUtility.DisplayProgressBar("Add Flammable → Scene Prefab Instances", scene.name, 0f);

            // BFS 掃描整個場景
            Queue<Transform> q = new Queue<Transform>();
            foreach (var r in roots) q.Enqueue(r.transform);

            int totalCount = 0;
            foreach (var r in roots) totalCount += r.GetComponentsInChildren<Transform>(true).Length;
            int idx = 0;

            while (q.Count > 0)
            {
                var t = q.Dequeue();
                foreach (Transform c in t) q.Enqueue(c);

                idx++;
                if (idx % 50 == 0)
                    EditorUtility.DisplayProgressBar("Add Flammable → Scene Prefab Instances", t.name, (float)idx / Mathf.Max(1, totalCount));

#if UNITY_2018_3_OR_NEWER
                // 只處理「Prefab 實例」(Hierarchy 內)
                if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                {
                    skippedNonPrefab++;
                    continue;
                }
#endif
                // 取得或新增 Flammable（只動場景實例，不 Apply 回資產）
                var f = t.GetComponent<Flammable>();
                if (f == null)
                {
                    f = Undo.AddComponent<Flammable>(t.gameObject);
                }
                else
                {
                    Undo.RecordObject(f, "Overwrite Flammable");
                }

                // 套用固定參數
                f.ignitionHeat = 100f;
                f.heatDissipation = 12f;
                f.fuel = 1000f;
                f.spreadRadius = 4f;
                f.heatPerSecond = 50f;
                f.flammableMask = fireMask;     // 指到 FireLayer
                f.maxHeat = 500f;

                f.firePrefab = firePrefab;
                f.residualSmokePrefab = null;
                f.fireAnchor = null;

                // 其他未提到 → 設 0 / None
                f.heat = 0f;

                EditorUtility.SetDirty(f);
                touched++;
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            EditorSceneManager.MarkSceneDirty(scene);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "完成",
            $"場景：{scene.name}\n" +
            $"已設定的 Prefab 實例：{touched} 個\n" +
            $"略過的非 Prefab 物件：{skippedNonPrefab} 個\n\n" +
            $"Fire Prefab：{FIRE_PREFAB_PATH}\n" +
            $"Flammable Mask：{FIRE_LAYER_NAME}",
            "OK");
    }
}
#endif

