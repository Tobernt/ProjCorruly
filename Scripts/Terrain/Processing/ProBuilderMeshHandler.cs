using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;

public class ProBuilderMeshHandler
{
    public static ProBuilderMesh CreateProBuilderMesh()
    {
        GameObject go = new GameObject("Generated ProBuilderMesh");
        ProBuilderMesh pbMesh = go.AddComponent<ProBuilderMesh>();

        return pbMesh;
    }

    public static void ApplyMesh(ProBuilderMesh pbMesh, Mesh unityMesh)
    {
        if (pbMesh == null || unityMesh == null) return;

        pbMesh.Clear();
        pbMesh.positions = new List<Vector3>(unityMesh.vertices);

        // ✅ Fix: Convert UVs to Vector4
        List<Vector4> uvList = new List<Vector4>();
        foreach (Vector2 uv in unityMesh.uv)
        {
            uvList.Add(new Vector4(uv.x, uv.y, 0, 0)); // Convert Vector2 to Vector4
        }

        // ✅ Fix: Apply corrected UVs
        pbMesh.SetUVs(0, uvList);

        pbMesh.ToMesh();
        pbMesh.Refresh();
    }
}
