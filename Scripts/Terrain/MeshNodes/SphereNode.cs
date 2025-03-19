using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class SphereNode : MeshNode
{
    [SerializeField] public int radius = 1;       // ✅ Adjustable radius
    [SerializeField] public int subdivisions = 4; // ✅ Determines smoothness

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null)
        {
            pbMesh = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, radius, subdivisions);
        }
        else
        {
            pbMesh.Clear();
            pbMesh = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, radius, subdivisions);
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();
        Debug.Log($"✅ SphereNode: Generated Sphere with Radius={radius}, Subdivisions={subdivisions}");
        return pbMesh;
    }
}
