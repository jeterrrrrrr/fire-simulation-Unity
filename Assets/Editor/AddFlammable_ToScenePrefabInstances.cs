#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class AddFlammable_ToScenePrefabInstances
{
    // ===== �T�w�]�w =====
    const string FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab";
    const string FIRE_LAYER_NAME = "FireLayer";

    // �ؼЮڸ��|�]Scene ���h�^
    static readonly string[] TARGET_PATH = { "Structure_02", "Interior" };

    // �ݦb Interior ���u�����vFlammable ���۹���|�]�۹�� Interior�^
    static readonly string[][] EXCLUDE_REL_PATHS = new[]
    {
        new[] { "Lobby", "Ceiling" },
        new[] { "2nd Floor", "Ceiling" },

        // 3�ӡG�䴩 3rd/3nd ��ةR�W
        new[] { "3rd Floor", "Ceiling" },
        new[] { "3nd Floor", "Ceiling" },

        // 4�ӡG�䴩 4th/4nd ��ةR�W
        new[] { "4th Floor", "Ceiling" },
        new[] { "4nd Floor", "Ceiling" },

        // 5�ӡG�䴩 5th/5nd ��ةR�W
        new[] { "5th Floor", "Ceiling" },
        new[] { "5nd Floor", "Ceiling" },
    };

    [MenuItem("Tools/Fire/Add Flammable �� Only 'Structure_02/Interior' (Exclude Ceilings) & Remove Others")]
    public static void AddToInteriorAndRemoveElsewhere()
    {
        // ���J Fire Prefab
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FIRE_PREFAB_PATH);
        if (firePrefab == null)
        {
            EditorUtility.DisplayDialog("���~", $"�䤣�� Fire Prefab�G\n{FIRE_PREFAB_PATH}", "OK");
            return;
        }

        // �p�� FireLayer �� LayerMask�]�䤣��]�i�~��A�|�]�� 0�^
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

        // ��� Interior �ڸ`�I
        var interior = FindByPath(scene, TARGET_PATH);
        if (interior == null)
        {
            EditorUtility.DisplayDialog(
                "�䤣��ؼ�",
                $"�b�����u{scene.name}�v���䤣����|�G\n{string.Join("/", TARGET_PATH)}",
                "OK");
            return;
        }

        // �b Interior ���U�A�إߡu�ݱư����϶��v���X
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

            // �έp�ƶq�Ω�i�ױ�
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
                    // �Y�b Interior�A�����u�ư��϶��v���� �� �R�� Flammable
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
                    // �u�B�z�uPrefab ��ҡv
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

                    // ===== �M�T�w�Ѽ� =====
                    f.ignitionHeat = 150f;
                    f.heatDissipation = 5f;
                    f.fuel = 600f;
                    f.spreadRadius = 4f;
                    f.heatPerSecond = 15f;
                    f.flammableMask = fireMask;        // ���� FireLayer
                    f.maxHeat = 400f;
                    f.selfHeatPerSecond = 7f;

                    f.firePrefab = firePrefab;
                    f.residualSmokePrefab = null;
                    f.fireAnchor = null;

                    // ��L������ �� �] 0 / None
                    f.heat = 0f;

                    EditorUtility.SetDirty(f);
                    addedOrOverwritten++;
                }
                else
                {
                    // �D Interior�G�M�� Flammable�]�ȧR����������^
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
            $"�ؼСG{string.Join("/", TARGET_PATH)}\n\n" +
            $"�w�s�W/�мg�]Prefab ��ҡ^Flammable�G{addedOrOverwritten} ��\n" +
            $"�w�����]Interior �U�ư��ϡ^�G{removedExcluded} ��\n" +
            $"�w�����]�D�ؼаϰ�^�G{removedOutside} ��\n" +
            $"���L���D Prefab ����]�b�ؼаϰ�^�G{skippedNonPrefab} ��\n\n" +
            $"Fire Prefab�G{FIRE_PREFAB_PATH}\n" +
            $"Flammable Mask�G{FIRE_LAYER_NAME}",
            "OK");
    }

    /// �b Scene �ھڧ�����|�M�� Transform�A�Ҧp ["Structure_02","Interior"]
    static Transform FindByPath(Scene scene, string[] path)
    {
        if (path == null || path.Length == 0) return null;

        // ��Ĥ@�h
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

        // �v�h�U�h��
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

    /// �b�Y�� root�]�p Interior�^�U�A�̬۹���|�M�� Transform�A�Ҧp ["Lobby","Ceiling"]
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
