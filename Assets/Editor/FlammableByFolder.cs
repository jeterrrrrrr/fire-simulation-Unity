#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public static class FlammableByFolder
{
    // === 你需要改這裡 ===
    static readonly string TARGET_LAYER_NAME = "Fire";
    // 若知道確切路徑，填路徑；不知道就留空，並改下面 NAME。
    static readonly string DEFAULT_FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab"; // TODO: 改成你的實際路徑
    static readonly string DEFAULT_FIRE_PREFAB_NAME = "SmallFires";                              // TODO: 或改成你的名稱

    // 可選：沒 Collider 時自動補
    static bool autoAddBoxCollider = false;

    static readonly string[] CATEGORIES = { "Walls", "Floors", "Ceiling", "Furniture" };

    struct Preset { public float ignitionHeat, heatDissipation, fuel, spreadRadius, heatPerSecond; }
    static readonly Dictionary<string, Preset> Presets = new()
    {
        ["Walls"] = new Preset { ignitionHeat = 160f, heatDissipation = 15f, fuel = 80f, spreadRadius = 2.5f, heatPerSecond = 35f },
        ["Floors"] = new Preset { ignitionHeat = 140f, heatDissipation = 14f, fuel = 100f, spreadRadius = 2.0f, heatPerSecond = 38f },
        ["Ceiling"] = new Preset { ignitionHeat = 130f, heatDissipation = 16f, fuel = 120f, spreadRadius = 3.0f, heatPerSecond = 42f },
        ["Furniture"] = new Preset { ignitionHeat = 90f, heatDissipation = 12f, fuel = 180f, spreadRadius = 1.5f, heatPerSecond = 45f },
    };

    const string FLAMMABLE_TYPE_NAME = "Flammable";
    static System.Type FType => System.AppDomain.CurrentDomain
        .GetAssemblies().SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == FLAMMABLE_TYPE_NAME);

    static int FireLayer => LayerMask.NameToLayer(TARGET_LAYER_NAME);
    static GameObject _cachedFirePrefab;

    // ============ 功能表 ============
    [MenuItem("Tools/Fire/Apply Flammable Presets → Selected Folders/Prefabs")]
    public static void ApplyToSelectedFoldersOrPrefabs()
    {
        if (!CheckReady()) return;

        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "請在 Project 視窗選資料夾或 Prefab 再執行。", "OK");
            return;
        }

        var paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        var prefabPaths = paths.SelectMany(p =>
        {
            if (AssetDatabase.IsValidFolder(p))
                return AssetDatabase.FindAssets("t:Prefab", new[] { p }).Select(AssetDatabase.GUIDToAssetPath);
            return AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(GameObject) ? new[] { p } : new string[0];
        }).Distinct();

        int count = 0;
        foreach (var path in prefabPaths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            ProcessPrefabAsset(go, path);
            count++;
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已處理 Prefab：{count} 個。", "OK");
    }

    [MenuItem("Tools/Fire/Apply Flammable Presets → Whole Scene (by prefab source path)")]
    public static void ApplyToWholeSceneBySourceFolder()
    {
        if (!CheckReady()) return;

        var scene = SceneManager.GetActiveScene();
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                var go = tr.gameObject;
                string srcPath = GetPrefabAssetPath(go); // 不是 prefab 實例可能為空
                ProcessGameObject(go, srcPath, applyToPrefabAsset: false);
                count++;
            }
        }
        EditorUtility.DisplayDialog("完成", $"場景「{scene.name}」已處理物件：約 {count} 個。", "OK");
    }

    // ============ 內部流程 ============
    static void ProcessPrefabAsset(GameObject prefabAsset, string assetPath)
    {
        foreach (var tr in prefabAsset.GetComponentsInChildren<Transform>(true))
            ProcessGameObject(tr.gameObject, assetPath, applyToPrefabAsset: true);
        EditorUtility.SetDirty(prefabAsset);
    }

    static void ProcessGameObject(GameObject go, string assetPath, bool applyToPrefabAsset)
    {
        if (FireLayer >= 0) SetLayerRecursive(go, FireLayer);
        if (autoAddBoxCollider && go.GetComponent<Collider>() == null)
            Undo.AddComponent<BoxCollider>(go);

        // 加或取得 Flammable
        var comp = go.GetComponent(FType);
        if (comp == null) { Undo.AddComponent(go, FType); comp = go.GetComponent(FType); }

        var so = new SerializedObject((Component)comp);

        // 依資料夾套數值
        var cat = MatchCategory(assetPath);
        if (cat != null) ApplyPresetToSerializedObject(so, Presets[cat]);

        // 自動填 Fire Prefab
        var firePrefab = GetDefaultFirePrefab();
        if (firePrefab != null) SetObjectIfExists(so, "firePrefab", firePrefab);

        // 自動填 Flammable Mask = Fire 層
        if (FireLayer >= 0) SetLayerMaskBits(so, "flammableMask", 1 << FireLayer);

        so.ApplyModifiedProperties();
        if (applyToPrefabAsset) EditorUtility.SetDirty(go);
    }

    static void ApplyPresetToSerializedObject(SerializedObject so, Preset p)
    {
        SetFloatIfExists(so, "ignitionHeat", p.ignitionHeat);
        SetFloatIfExists(so, "heatDissipation", p.heatDissipation);
        SetFloatIfExists(so, "fuel", p.fuel);
        SetFloatIfExists(so, "spreadRadius", p.spreadRadius);
        SetFloatIfExists(so, "heatPerSecond", p.heatPerSecond);
    }

    // --- Helpers for SerializedObject ---
    static void SetFloatIfExists(SerializedObject so, string propName, float v)
    {
        var prop = so.FindProperty(propName);
        if (prop != null && prop.propertyType == SerializedPropertyType.Float)
            prop.floatValue = v;
    }

    static void SetObjectIfExists(SerializedObject so, string propName, Object obj)
    {
        var prop = so.FindProperty(propName);
        if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
            prop.objectReferenceValue = obj;
    }

    // LayerMask 在序列化中是整數位元
    static void SetLayerMaskBits(SerializedObject so, string propName, int bits)
    {
        var prop = so.FindProperty(propName);
        if (prop != null && (prop.propertyType == SerializedPropertyType.Integer || prop.propertyType == SerializedPropertyType.LayerMask))
            prop.intValue = bits;
    }

    static string MatchCategory(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        var lower = assetPath.Replace('\\', '/').ToLowerInvariant();
        foreach (var c in CATEGORIES)
            if (lower.Contains("/" + c.ToLowerInvariant() + "/"))
                return c;
        return null;
    }

    static string GetPrefabAssetPath(GameObject instance)
    {
#if UNITY_2018_3_OR_NEWER
        var src = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        if (src != null) return AssetDatabase.GetAssetPath(src);
        return null;
#else
        return null;
#endif
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    // 找預設火焰 Prefab（優先用路徑，找不到就用名稱搜尋）
    static GameObject GetDefaultFirePrefab()
    {
        if (_cachedFirePrefab != null) return _cachedFirePrefab;

        if (!string.IsNullOrEmpty(DEFAULT_FIRE_PREFAB_PATH))
        {
            _cachedFirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DEFAULT_FIRE_PREFAB_PATH);
            if (_cachedFirePrefab != null) return _cachedFirePrefab;
        }

        if (!string.IsNullOrEmpty(DEFAULT_FIRE_PREFAB_NAME))
        {
            var guids = AssetDatabase.FindAssets($"t:Prefab {DEFAULT_FIRE_PREFAB_NAME}");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.name == DEFAULT_FIRE_PREFAB_NAME)
                {
                    _cachedFirePrefab = go;
                    return _cachedFirePrefab;
                }
            }
        }

        Debug.LogWarning("FlammableByFolder: 找不到預設 Fire Prefab，請設定 DEFAULT_FIRE_PREFAB_PATH 或 NAME。");
        return null;
    }

    static bool CheckReady()
    {
        if (FType == null)
        {
            EditorUtility.DisplayDialog("錯誤", $"找不到類別 {FLAMMABLE_TYPE_NAME}，請先把 Flammable.cs 放進專案。", "OK");
            return false;
        }
        if (FireLayer < 0)
        {
            EditorUtility.DisplayDialog("提示", $"尚未建立 Layer「{TARGET_LAYER_NAME}」。請到 Project Settings → Tags and Layers 新增。", "OK");
            return false;
        }
        return true;
    }
}
#endif
