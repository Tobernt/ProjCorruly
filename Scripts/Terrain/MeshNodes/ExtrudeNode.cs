using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;

public class ExtrudeNode : MeshNode
{
    [SerializeField] public float extrudeDistance = 0.2f; // Exposed to Inspector

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null)
        {
            Debug.LogError("❌ ExtrudeNode: Input mesh is NULL!");
            return null;
        }

        if (pbMesh.faceCount == 0)
        {
            Debug.LogWarning("⚠️ ExtrudeNode: No faces found in mesh. Cannot extrude.");
            return pbMesh;
        }

        Debug.Log($"🛠 Extruding Mesh: Distance={extrudeDistance}");

        List<Face> faces = new List<Face>(pbMesh.faces);
        pbMesh.Extrude(faces, ExtrudeMethod.IndividualFaces, extrudeDistance);

        pbMesh.ToMesh();
        pbMesh.Refresh();
        return pbMesh;
    }
}
