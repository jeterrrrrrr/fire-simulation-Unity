#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class AddFlammable_ToScenePrefabInstances
{
    // �A���T�w�]�w
    const string FIRE_PREFAB_PATH = "Assets/PyroParticles/Prefab/Prefab/SmallFires.prefab";
    const string FIRE_LAYER_NAME = "FireLayer";

    [MenuItem("Tools/Fire/Add Flammable �� Scene Prefab Instances (Hierarchy)")]
    public static void AddToSceneInstances()
    {
        // ���J Fire Prefab
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FIRE_PREFAB_PATH);
        if (firePrefab == null)
        {
            EditorUtility.DisplayDialog("���~", $"�䤣�� Fire Prefab�G\n{FIRE_PREFAB_PATH}", "OK");
            return;
        }

        // �p�� FireLayer �� LayerMask
        int fireMask = LayerMask.GetMask(FIRE_LAYER_NAME);
        if (fireMask == 0)
        {
            bool cont = EditorUtility.DisplayDialog(
                "����",
                $"�䤣��ϼh�u{FIRE_LAYER_NAME}�v�C\n�Х��� Project Settings �� Tags and Layers �s�W�C\n\n���n�~��]flammableMask �|�]�� 0�^�H",
                "���M����", "����");
            if (!cont) return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("���~", "�ثe�S���}�Ҥ��� Scene�C", "OK");
            return;
        }

        var roots = scene.GetRootGameObjects();
        int touched = 0, skippedNonPrefab = 0;

        try
        {
            Undo.IncrementCurrentGroup();
            int opGroup = Undo.GetCurrentGroup();

            EditorUtility.DisplayProgressBar("Add Flammable �� Scene Prefab Instances", scene.name, 0f);

            // BFS ���y��ӳ���
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
                    EditorUtility.DisplayProgressBar("Add Flammable �� Scene Prefab Instances", t.name, (float)idx / Mathf.Max(1, totalCount));

#if UNITY_2018_3_OR_NEWER
                // �u�B�z�uPrefab ��ҡv(Hierarchy ��)
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

                // �M�ΩT�w�Ѽ�
                f.ignitionHeat = 100f;
                f.heatDissipation = 12f;
                f.fuel = 1000f;
                f.spreadRadius = 4f;
                f.heatPerSecond = 50f;
                f.flammableMask = fireMask;     // ���� FireLayer
                f.maxHeat = 500f;

                f.firePrefab = firePrefab;
                f.residualSmokePrefab = null;
                f.fireAnchor = null;

                // ��L������ �� �] 0 / None
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
            "����",
            $"�����G{scene.name}\n" +
            $"�w�]�w�� Prefab ��ҡG{touched} ��\n" +
            $"���L���D Prefab ����G{skippedNonPrefab} ��\n\n" +
            $"Fire Prefab�G{FIRE_PREFAB_PATH}\n" +
            $"Flammable Mask�G{FIRE_LAYER_NAME}",
            "OK");
    }
}
#endif

