using UnityEngine;
using UnityEngine.ProBuilder;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeshExporter
{
    public void ExportMesh(ProBuilderMesh mesh)
    {
        if (mesh == null)
        {
            Debug.LogError("MeshExporterNode received a null mesh for export!");
            return;
        }

        string path = "Assets/ExportedMesh.asset";
        Mesh unityMesh = new Mesh();
        mesh.ToMesh();
        mesh.Refresh();
        mesh.GetComponent<MeshFilter>().mesh = unityMesh;

#if UNITY_EDITOR
        AssetDatabase.CreateAsset(unityMesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Mesh exported to: {path}");
#else
        Debug.LogError("Mesh export can only be performed in the Unity Editor.");
#endif
    }
}
