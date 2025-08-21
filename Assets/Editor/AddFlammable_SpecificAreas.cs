#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class AddFlammable_SpecificAreas
{
    // ===== �T�w�]�w�]�u�ΧA���e���ƭȡ^=====
    const string FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab";
    const string FIRE_LAYER_NAME = "FireLayer";

    // �ؼЮڸ��|�]Scene ���h�^
    static readonly string[] INTERIOR_PATH = { "Structure_02", "Interior" };

    // �զW��G�C�Ӱϰ�U�n�B�z���u���O��Ƨ��v�M��
    // Lobby & 2F�GWalls/Floors/Furniture
    static readonly string[][] LOBBY_REL = { new[] { "Lobby" } };
    static readonly string[] LOBBY_CATS = { "Walls", "Floors", "Furniture" };

    static readonly string[][] F2_REL = { new[] { "2nd Floor", "Apartment_01" } };
    static readonly string[] F2_CATS = { "Walls", "Floors", "Furniture" };

    // 3F/4F/5F�G�L Furniture �� �u�B�z Walls/Floors
    static readonly string[][] F3_REL = { new[] { "3rd Floor", "Apartment_01" } };
    static readonly string[] F3_CATS = { "Walls", "Floors" };

    static readonly string[][] F4_REL = { new[] { "4th Floor", "Apartment_01" } };
    static readonly string[] F4_CATS = { "Walls", "Floors" };

    static readonly string[][] F5_REL = { new[] { "5th Floor", "Apartment_01" } };
    static readonly string[] F5_CATS = { "Walls", "Floors" };

    [MenuItem("Tools/Fire/Add Flammable �� Specific Areas by Floor (Whitelist) & Remove Others")]
    public static void ApplyOnlyOnSpecifiedAreasAndCleanOthers()
    {
        // Fire Prefab
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FIRE_PREFAB_PATH);
        if (firePrefab == null)
        {
            EditorUtility.DisplayDialog("���~", $"�䤣�� Fire Prefab�G\n{FIRE_PREFAB_PATH}", "OK");
            return;
        }

        // LayerMask
        int fireMask = LayerMask.GetMask(FIRE_LAYER_NAME);
        if (fireMask == 0)
        {
            bool cont = EditorUtility.DisplayDialog(
                "����",
                $"�䤣��ϼh�u{FIRE_LAYER_NAME}�v�C\n�Х��� Project Settings �� Tags and Layers �s�W�C\n\n���n�~��]flammableMask �N�]�� 0�^�H",
                "���M����", "����");
            if (!cont) return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("���~", "�ثe�S���}�Ҥ��� Scene�C", "OK");
            return;
        }

        // ��� Interior
        var interior = FindByPath(scene, INTERIOR_PATH);
        if (interior == null)
        {
            EditorUtility.DisplayDialog(
                "�䤣��ؼ�",
                $"�b�����u{scene.name}�v���䤣����|�G\n{string.Join("/", INTERIOR_PATH)}",
                "OK");
            return;
        }

        // ===== �إߥզW��ڶ��X�]��ڬ��U�ϰ�U�� Walls/Floors(/Furniture) �`�I Transform�^=====
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

            // �i�ױ��έp
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
                    // �u�� Prefab ��Ұʧ@�]�Y�n�s�D Prefab �@�_�A���ѱ����϶��^
                    if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                    {
                        skippedNonPrefab++;
                        continue;
                    }
#endif
                    // ���o�ηs�W Flammable�]�u�ʳ�����ҡA�� Apply �^�겣�^
                    var f = t.GetComponent<Flammable>();
                    if (f == null)
                    {
                        f = Undo.AddComponent<Flammable>(t.gameObject);
                    }
                    else
                    {
                        Undo.RecordObject(f, "Overwrite Flammable");
                    }

                    // ===== �M�T�w�Ѽơ]�u�ΧA���]�w�^=====
                    f.ignitionHeat = 120f;
                    f.heatDissipation = 7f;
                    f.fuel = 300f;
                    f.spreadRadius = 3.5f;
                    f.heatPerSecond = 30f;
                    f.flammableMask = fireMask;        // ���� FireLayer
                    f.maxHeat = 400f;
                    f.selfHeatPerSecond = 9f;

                    f.firePrefab = firePrefab;
                    f.residualSmokePrefab = null;
                    f.fireAnchor = null;

                    // ��L������ �� �] 0
                    f.heat = 0f;

                    EditorUtility.SetDirty(f);
                    addedOrOverwritten++;
                }
                else
                {
                    // ���b�զW��G���� Flammable�]�ȧR����������^
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
            "����",
            $"�����G{scene.name}\n" +
            $"�զW��ϰ�G\n" +
            $" - Lobby �� Walls/Floors/Furniture\n" +
            $" - 2nd Floor / Apartment_01 �� Walls/Floors/Furniture\n" +
            $" - 3rd/4th/5th Floor / Apartment_01 �� Walls/Floors\n\n" +
            $"�w�s�W/�мg�]Prefab ��ҡ^Flammable�G{addedOrOverwritten} ��\n" +
            $"�w�����]�D�զW��ϰ�^Flammable�G{removedOutside} ��\n" +
            $"���L���D Prefab ����]�b�զW��ϰ�^�G{skippedNonPrefab} ��\n\n" +
            $"Fire Prefab�G{FIRE_PREFAB_PATH}\n" +
            $"Flammable Mask�G{FIRE_LAYER_NAME}",
            "OK");
    }

    // ===== Helper =====

    /// �b Scene �ھڧ�����|�M�� Transform�A�Ҧp ["Structure_02","Interior"]
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

    /// �b�Y�� root �U�A�̬۹���|�M�� Transform�A�Ҧp ["3rd Floor","Apartment_01"]
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

    /// �u�b��e�h�䪽���l����
    static Transform FindDirectChild(Transform parent, string childName)
    {
        foreach (Transform c in parent)
            if (c.name == childName) return c;
        return null;
    }
}
#endif
