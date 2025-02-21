using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class CylinderNode : MeshNode
{
    public int radius = 5;
    public int height = 10;
    public int radialSubdivisions = 16;
    public int heightCuts = 3; // ✅ Added missing parameter

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh existingMesh)
    {
        if (existingMesh == null)
        {
            Debug.Log($"🛠 Generating new Cylinder Mesh: Radius={radius}, Height={height}, Radial Subdivisions={radialSubdivisions}, Height Cuts={heightCuts}");
            existingMesh = ShapeGenerator.GenerateCylinder(PivotLocation.Center, radius, height, heightCuts, radialSubdivisions, 1);
        }
        else
        {
            Debug.Log($"🔄 Updating Cylinder Mesh: Radius={radius}, Height={height}, Radial Subdivisions={radialSubdivisions}, Height Cuts={heightCuts}");
            existingMesh.Clear();
            existingMesh = ShapeGenerator.GenerateCylinder(PivotLocation.Center, radius, height, heightCuts, radialSubdivisions, 1);
        }

        existingMesh.ToMesh();
        existingMesh.Refresh();
        return existingMesh;
    }
}
