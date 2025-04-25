using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RevertToStandardMaterials : EditorWindow
{
    [MenuItem("Tools/Revert URP Materials to Standard")]
    public static void RevertURPMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat.shader.name.Contains("Universal Render Pipeline"))
            {
                Debug.Log($"[Convert] Reverting {mat.name} at {path}");

                Texture mainTex = mat.GetTexture("_BaseMap");
                Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;

                mat.shader = Shader.Find("Standard");

                if (mainTex) mat.SetTexture("_MainTex", mainTex);
                mat.SetColor("_Color", color);

                // Optionally try to convert metallic/smoothness
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", mat.GetFloat("_Metallic"));

                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Convert] Finished. Reverted {converted} materials.");
    }
}
