using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class PrimitiveNode : MeshNode
{
    public enum PrimitiveType
    {
        Cube,
        Sphere,
        Cylinder,
        Plane,
        Torus,
        Cone
    }

    public PrimitiveType primitiveType = PrimitiveType.Cube;

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null)
        {
            GameObject go = new GameObject($"{primitiveType} Primitive");
            pbMesh = go.AddComponent<ProBuilderMesh>();
        }
        else
        {
            pbMesh.Clear();
        }

        switch (primitiveType)
        {
            case PrimitiveType.Cube:
                pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
                break;
            case PrimitiveType.Sphere:
                pbMesh = ShapeGenerator.CreateShape(ShapeType.Sphere);
                break;
            case PrimitiveType.Cylinder:
                pbMesh = ShapeGenerator.CreateShape(ShapeType.Cylinder);
                break;
            case PrimitiveType.Plane:
                pbMesh = ShapeGenerator.CreateShape(ShapeType.Plane);
                break;
            case PrimitiveType.Torus:
                pbMesh = ShapeGenerator.GenerateTorus(
                    PivotLocation.Center, // Center pivot
                    16,                   // Number of radial segments
                    8,                    // Number of tube segments
                    1.0f,                 // Major radius (outer radius)
                    0.3f,                 // Minor radius (tube thickness)
                    true,                 // Smooth shading
                    1.0f,                 // Horizontal UV scale
                    1.0f,                 // Vertical UV scale
                    true                  // Flip normals
                );
                break;
            case PrimitiveType.Cone:
                pbMesh = ShapeGenerator.GenerateCone(PivotLocation.Center, 1.0f, 2.0f, 16);
                break;
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();
        return pbMesh;
    }
}
