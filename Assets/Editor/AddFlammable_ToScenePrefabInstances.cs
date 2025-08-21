#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class AddFlammable_ToScenePrefabInstances
{
    // ===== 固定設定 =====
    const string FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab";
    const string FIRE_LAYER_NAME = "FireLayer";

    // 目標根路徑（Scene 階層）
    static readonly string[] TARGET_PATH = { "Structure_02", "Interior" };

    // 需在 Interior 內「移除」Flammable 的相對路徑（相對於 Interior）
    static readonly string[][] EXCLUDE_REL_PATHS = new[]
    {
        new[] { "Lobby", "Ceiling" },
        new[] { "2nd Floor", "Ceiling" },

        // 3樓：支援 3rd/3nd 兩種命名
        new[] { "3rd Floor", "Ceiling" },
        new[] { "3nd Floor", "Ceiling" },

        // 4樓：支援 4th/4nd 兩種命名
        new[] { "4th Floor", "Ceiling" },
        new[] { "4nd Floor", "Ceiling" },

        // 5樓：支援 5th/5nd 兩種命名
        new[] { "5th Floor", "Ceiling" },
        new[] { "5nd Floor", "Ceiling" },
    };

    [MenuItem("Tools/Fire/Add Flammable → Only 'Structure_02/Interior' (Exclude Ceilings) & Remove Others")]
    public static void AddToInteriorAndRemoveElsewhere()
    {
        // 載入 Fire Prefab
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FIRE_PREFAB_PATH);
        if (firePrefab == null)
        {
            EditorUtility.DisplayDialog("錯誤", $"找不到 Fire Prefab：\n{FIRE_PREFAB_PATH}", "OK");
            return;
        }

        // 計算 FireLayer 的 LayerMask（找不到也可繼續，會設為 0）
        int fireMask = LayerMask.GetMask(FIRE_LAYER_NAME);
        if (fireMask == 0)
        {
            bool cont = EditorUtility.DisplayDialog(
                "提醒",
                $"找不到圖層「{FIRE_LAYER_NAME}」。\n請先到 Project Settings → Tags and Layers 新增。\n\n仍要繼續（flammableMask 將設為 0）？",
                "仍然執行", "取消");
            if (!cont) return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("錯誤", "目前沒有開啟中的 Scene。", "OK");
            return;
        }

        // 找到 Interior 根節點
        var interior = FindByPath(scene, TARGET_PATH);
        if (interior == null)
        {
            EditorUtility.DisplayDialog(
                "找不到目標",
                $"在場景「{scene.name}」中找不到路徑：\n{string.Join("/", TARGET_PATH)}",
                "OK");
            return;
        }

        // 在 Interior 底下，建立「需排除的區塊」集合
        var excludedRoots = new HashSet<Transform>();
        foreach (var rel in EXCLUDE_REL_PATHS)
        {
            var tr = FindChildByPath(interior, rel);
            if (tr != null) excludedRoots.Add(tr);
        }

        bool IsUnderExcluded(Transform t)
        {
            foreach (var ex in excludedRoots)
            {
                if (t == ex || t.IsChildOf(ex)) return true;
            }
            return false;
        }

        int addedOrOverwritten = 0;
        int removedOutside = 0;
        int removedExcluded = 0;
        int skippedNonPrefab = 0;

        try
        {
            Undo.IncrementCurrentGroup();
            int opGroup = Undo.GetCurrentGroup();

            // 統計數量用於進度條
            var allTransforms = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
                allTransforms.AddRange(root.GetComponentsInChildren<Transform>(true));

            EditorUtility.DisplayProgressBar("Processing Flammable", scene.name, 0f);

            for (int i = 0; i < allTransforms.Count; i++)
            {
                var t = allTransforms[i];
                float progress = (float)(i + 1) / Mathf.Max(1, allTransforms.Count);
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Processing Flammable", t.name, progress);

                bool underInterior = t == interior || t.IsChildOf(interior);

                if (underInterior)
                {
                    // 若在 Interior，但位於「排除區塊」之內 → 刪除 Flammable
                    if (IsUnderExcluded(t))
                    {
                        var comps = t.GetComponents<Flammable>();
                        if (comps != null && comps.Length > 0)
                        {
                            foreach (var comp in comps)
                            {
                                Undo.DestroyObjectImmediate(comp);
                                removedExcluded++;
                            }
                        }
                        continue;
                    }

#if UNITY_2018_3_OR_NEWER
                    // 只處理「Prefab 實例」
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

                    // ===== 套固定參數 =====
                    f.ignitionHeat = 150f;
                    f.heatDissipation = 5f;
                    f.fuel = 600f;
                    f.spreadRadius = 4f;
                    f.heatPerSecond = 15f;
                    f.flammableMask = fireMask;        // 指到 FireLayer
                    f.maxHeat = 400f;
                    f.selfHeatPerSecond = 7f;

                    f.firePrefab = firePrefab;
                    f.residualSmokePrefab = null;
                    f.fireAnchor = null;

                    // 其他未提到 → 設 0 / None
                    f.heat = 0f;

                    EditorUtility.SetDirty(f);
                    addedOrOverwritten++;
                }
                else
                {
                    // 非 Interior：清掉 Flammable（僅刪場景內元件）
                    var fs = t.GetComponents<Flammable>();
                    if (fs != null && fs.Length > 0)
                    {
                        foreach (var comp in fs)
                        {
                            Undo.DestroyObjectImmediate(comp);
                            removedOutside++;
                        }
                    }
                }
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
            $"目標：{string.Join("/", TARGET_PATH)}\n\n" +
            $"已新增/覆寫（Prefab 實例）Flammable：{addedOrOverwritten} 個\n" +
            $"已移除（Interior 下排除區）：{removedExcluded} 個\n" +
            $"已移除（非目標區域）：{removedOutside} 個\n" +
            $"略過的非 Prefab 物件（在目標區域）：{skippedNonPrefab} 個\n\n" +
            $"Fire Prefab：{FIRE_PREFAB_PATH}\n" +
            $"Flammable Mask：{FIRE_LAYER_NAME}",
            "OK");
    }

    /// 在 Scene 根據完整路徑尋找 Transform，例如 ["Structure_02","Interior"]
    static Transform FindByPath(Scene scene, string[] path)
    {
        if (path == null || path.Length == 0) return null;

        // 找第一層
        Transform current = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == path[0])
            {
                current = root.transform;
                break;
            }
        }
        if (current == null) return null;

        // 逐層下去找
        for (int i = 1; i < path.Length; i++)
        {
            var nextName = path[i];
            Transform next = null;
            foreach (Transform child in current)
            {
                if (child.name == nextName) { next = child; break; }
            }
            if (next == null) return null;
            current = next;
        }
        return current;
    }

    /// 在某個 root（如 Interior）下，依相對路徑尋找 Transform，例如 ["Lobby","Ceiling"]
    static Transform FindChildByPath(Transform root, string[] rel)
    {
        if (root == null || rel == null || rel.Length == 0) return null;
        Transform current = root;
        for (int i = 0; i < rel.Length; i++)
        {
            var name = rel[i];
            Transform next = null;
            foreach (Transform c in current)
            {
                if (c.name == name) { next = c; break; }
            }
            if (next == null) return null;
            current = next;
        }
        return current;
    }
}
#endif
