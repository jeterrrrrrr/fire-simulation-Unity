#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public static class FlammableByFolder
{
    // === �A�ݭn��o�� ===
    static readonly string TARGET_LAYER_NAME = "Fire";
    // �Y���D�T�����|�A����|�F�����D�N�d�šA�ç�U�� NAME�C
    static readonly string DEFAULT_FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab"; // TODO: �令�A����ڸ��|
    static readonly string DEFAULT_FIRE_PREFAB_NAME = "SmallFires";                              // TODO: �Χ令�A���W��

    // �i��G�S Collider �ɦ۰ʸ�
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

    // ============ �\��� ============
    [MenuItem("Tools/Fire/Apply Flammable Presets �� Selected Folders/Prefabs")]
    public static void ApplyToSelectedFoldersOrPrefabs()
    {
        if (!CheckReady()) return;

        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("����", "�Цb Project �������Ƨ��� Prefab �A����C", "OK");
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
        EditorUtility.DisplayDialog("����", $"�w�B�z Prefab�G{count} �ӡC", "OK");
    }

    [MenuItem("Tools/Fire/Apply Flammable Presets �� Whole Scene (by prefab source path)")]
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
                string srcPath = GetPrefabAssetPath(go); // ���O prefab ��ҥi�ର��
                ProcessGameObject(go, srcPath, applyToPrefabAsset: false);
                count++;
            }
        }
        EditorUtility.DisplayDialog("����", $"�����u{scene.name}�v�w�B�z����G�� {count} �ӡC", "OK");
    }

    // ============ �����y�{ ============
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

        // �[�Ψ��o Flammable
        var comp = go.GetComponent(FType);
        if (comp == null) { Undo.AddComponent(go, FType); comp = go.GetComponent(FType); }

        var so = new SerializedObject((Component)comp);

        // �̸�Ƨ��M�ƭ�
        var cat = MatchCategory(assetPath);
        if (cat != null) ApplyPresetToSerializedObject(so, Presets[cat]);

        // �۰ʶ� Fire Prefab
        var firePrefab = GetDefaultFirePrefab();
        if (firePrefab != null) SetObjectIfExists(so, "firePrefab", firePrefab);

        // �۰ʶ� Flammable Mask = Fire �h
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

    // LayerMask �b�ǦC�Ƥ��O��Ʀ줸
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

    // ��w�]���K Prefab�]�u���θ��|�A�䤣��N�ΦW�ٷj�M�^
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

        Debug.LogWarning("FlammableByFolder: �䤣��w�] Fire Prefab�A�г]�w DEFAULT_FIRE_PREFAB_PATH �� NAME�C");
        return null;
    }

    static bool CheckReady()
    {
        if (FType == null)
        {
            EditorUtility.DisplayDialog("���~", $"�䤣�����O {FLAMMABLE_TYPE_NAME}�A�Х��� Flammable.cs ��i�M�סC", "OK");
            return false;
        }
        if (FireLayer < 0)
        {
            EditorUtility.DisplayDialog("����", $"�|���إ� Layer�u{TARGET_LAYER_NAME}�v�C�Ш� Project Settings �� Tags and Layers �s�W�C", "OK");
            return false;
        }
        return true;
    }
}
#endif
