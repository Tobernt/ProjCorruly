using UnityEngine;
using UnityEngine.ProBuilder;

public class MaterialNode : MeshNode
{
    public Material assignedMaterial;

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null) return null;

        Renderer renderer = pbMesh.gameObject.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = pbMesh.gameObject.AddComponent<MeshRenderer>();
        }

        if (assignedMaterial != null)
        {
            renderer.sharedMaterial = assignedMaterial;
            Debug.Log($"✅ MaterialNode: Applied {assignedMaterial.name} to {pbMesh.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("⚠ MaterialNode: No material assigned.");
        }

        return pbMesh;
    }
}
