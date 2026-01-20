using UnityEditor;
using UnityEngine;
using System.IO;

public class ConvertAllShadersToStandard : EditorWindow
{
    [MenuItem("Tools/Shader/Convert All to Standard")]
    public static void ConvertShaders()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader != Shader.Find("Standard"))
            {
                mat.shader = Shader.Find("Standard");
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ 已將 {count} 個材質的 Shader 轉換為 Standard。");
    }

    [MenuItem("Tools/Shader/Convert All to Standard", true)]
    private static bool ValidateConvertShaders()
    {
        return !EditorApplication.isPlaying;
    }
}

