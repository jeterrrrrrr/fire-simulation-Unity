#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class AddFlammable_SpecificAreas
{
    // ===== 固定設定（沿用你先前的數值）=====
    const string FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab";
    const string FIRE_LAYER_NAME = "FireLayer";

    // 目標根路徑（Scene 階層）
    static readonly string[] INTERIOR_PATH = { "Structure_02", "Interior" };

    // 白名單：每個區域下要處理的「類別資料夾」清單
    // Lobby & 2F：Walls/Floors/Furniture
    static readonly string[][] LOBBY_REL = { new[] { "Lobby" } };
    static readonly string[] LOBBY_CATS = { "Walls", "Floors", "Furniture" };

    static readonly string[][] F2_REL = { new[] { "2nd Floor", "Apartment_01" } };
    static readonly string[] F2_CATS = { "Walls", "Floors", "Furniture" };

    // 3F/4F/5F：無 Furniture → 只處理 Walls/Floors
    static readonly string[][] F3_REL = { new[] { "3rd Floor", "Apartment_01" } };
    static readonly string[] F3_CATS = { "Walls", "Floors" };

    static readonly string[][] F4_REL = { new[] { "4th Floor", "Apartment_01" } };
    static readonly string[] F4_CATS = { "Walls", "Floors" };

    static readonly string[][] F5_REL = { new[] { "5th Floor", "Apartment_01" } };
    static readonly string[] F5_CATS = { "Walls", "Floors" };

    [MenuItem("Tools/Fire/Add Flammable → Specific Areas by Floor (Whitelist) & Remove Others")]
    public static void ApplyOnlyOnSpecifiedAreasAndCleanOthers()
    {
        // Fire Prefab
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FIRE_PREFAB_PATH);
        if (firePrefab == null)
        {
            EditorUtility.DisplayDialog("錯誤", $"找不到 Fire Prefab：\n{FIRE_PREFAB_PATH}", "OK");
            return;
        }

        // LayerMask
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

        // 找到 Interior
        var interior = FindByPath(scene, INTERIOR_PATH);
        if (interior == null)
        {
            EditorUtility.DisplayDialog(
                "找不到目標",
                $"在場景「{scene.name}」中找不到路徑：\n{string.Join("/", INTERIOR_PATH)}",
                "OK");
            return;
        }

        // ===== 建立白名單根集合（實際為各區域下的 Walls/Floors(/Furniture) 節點 Transform）=====
        var whitelistRoots = new HashSet<Transform>();
        void AddAreaRoots(string[][] areaRelPath, string[] cats)
        {
            foreach (var rel in areaRelPath)
            {
                var areaRoot = FindChildByPath(interior, rel);
                if (areaRoot == null) continue;
                foreach (var cat in cats)
                {
                    var catTr = FindDirectChild(areaRoot, cat);
                    if (catTr != null) whitelistRoots.Add(catTr);
                }
            }
        }

        AddAreaRoots(LOBBY_REL, LOBBY_CATS);
        AddAreaRoots(F2_REL, F2_CATS);
        AddAreaRoots(F3_REL, F3_CATS);
        AddAreaRoots(F4_REL, F4_CATS);
        AddAreaRoots(F5_REL, F5_CATS);

        bool IsUnderWhitelist(Transform t)
        {
            foreach (var w in whitelistRoots)
            {
                if (t == w || t.IsChildOf(w)) return true;
            }
            return false;
        }

        int addedOrOverwritten = 0;
        int removedOutside = 0;
        int skippedNonPrefab = 0;

        try
        {
            Undo.IncrementCurrentGroup();
            int opGroup = Undo.GetCurrentGroup();

            // 進度條統計
            var allTransforms = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
                allTransforms.AddRange(root.GetComponentsInChildren<Transform>(true));

            EditorUtility.DisplayProgressBar("Processing Flammable (Specific Areas by Floor)", scene.name, 0f);

            for (int i = 0; i < allTransforms.Count; i++)
            {
                var t = allTransforms[i];
                if (i % 50 == 0)
                {
                    float progress = (float)(i + 1) / Mathf.Max(1, allTransforms.Count);
                    EditorUtility.DisplayProgressBar("Processing Flammable (Specific Areas by Floor)", t.name, progress);
                }

                bool underInterior = t == interior || t.IsChildOf(interior);
                bool inWhitelist = underInterior && IsUnderWhitelist(t);

                if (inWhitelist)
                {
#if UNITY_2018_3_OR_NEWER
                    // 只對 Prefab 實例動作（若要連非 Prefab 一起，註解掉此區塊）
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

                    // ===== 套固定參數（沿用你的設定）=====
                    f.ignitionHeat = 120f;
                    f.heatDissipation = 7f;
                    f.fuel = 300f;
                    f.spreadRadius = 3.5f;
                    f.heatPerSecond = 30f;
                    f.flammableMask = fireMask;        // 指到 FireLayer
                    f.maxHeat = 400f;
                    f.selfHeatPerSecond = 9f;

                    f.firePrefab = firePrefab;
                    f.residualSmokePrefab = null;
                    f.fireAnchor = null;

                    // 其他未提到 → 設 0
                    f.heat = 0f;

                    EditorUtility.SetDirty(f);
                    addedOrOverwritten++;
                }
                else
                {
                    // 不在白名單：移除 Flammable（僅刪場景內元件）
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
            $"白名單區域：\n" +
            $" - Lobby → Walls/Floors/Furniture\n" +
            $" - 2nd Floor / Apartment_01 → Walls/Floors/Furniture\n" +
            $" - 3rd/4th/5th Floor / Apartment_01 → Walls/Floors\n\n" +
            $"已新增/覆寫（Prefab 實例）Flammable：{addedOrOverwritten} 個\n" +
            $"已移除（非白名單區域）Flammable：{removedOutside} 個\n" +
            $"略過的非 Prefab 物件（在白名單區域）：{skippedNonPrefab} 個\n\n" +
            $"Fire Prefab：{FIRE_PREFAB_PATH}\n" +
            $"Flammable Mask：{FIRE_LAYER_NAME}",
            "OK");
    }

    // ===== Helper =====

    /// 在 Scene 根據完整路徑尋找 Transform，例如 ["Structure_02","Interior"]
    static Transform FindByPath(Scene scene, string[] path)
    {
        if (path == null || path.Length == 0) return null;

        Transform current = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == path[0]) { current = root.transform; break; }
        }
        if (current == null) return null;

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

    /// 在某個 root 下，依相對路徑尋找 Transform，例如 ["3rd Floor","Apartment_01"]
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

    /// 只在當前層找直接子物件
    static Transform FindDirectChild(Transform parent, string childName)
    {
        foreach (Transform c in parent)
            if (c.name == childName) return c;
        return null;
    }
}
#endif
