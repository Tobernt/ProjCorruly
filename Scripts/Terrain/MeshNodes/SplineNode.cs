using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class SplineNode : MeshNode
{
    public List<Vector3> controlPoints = new List<Vector3>();
    public float roadWidth = 2f;
    public int subdivisions = 10;
    public float bridgeThreshold = 3f; // If road is too high, create bridge supports

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (controlPoints.Count < 2)
        {
            Debug.LogError("❌ SplineNode requires at least 2 control points!");
            return null;
        }

        GameObject go = new GameObject("Spline Road");
        pbMesh = go.AddComponent<ProBuilderMesh>();

        List<Vector3> vertices = new List<Vector3>();
        List<Face> faces = new List<Face>();

        for (int i = 0; i < subdivisions; i++)
        {
            float t = i / (float)(subdivisions - 1);
            Vector3 point = GetSplinePoint(t);

            if (point.y > bridgeThreshold) // Create bridge supports for high points
            {
                GenerateBridgeSupport(point);
            }

            Vector3 forward = (GetSplinePoint(t + 0.01f) - point).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            // Add vertices for left & right of road
            vertices.Add(point + right * roadWidth * 0.5f);
            vertices.Add(point - right * roadWidth * 0.5f);

            // Generate faces
            if (i < subdivisions - 1)
            {
                int index = i * 2;
                faces.Add(new Face(new int[]
                {
                    index, index + 2, index + 1,
                    index + 1, index + 2, index + 3
                }));
            }
        }

        // Apply ProBuilder Mesh Data
        pbMesh.Clear();
        pbMesh.positions = vertices;
        pbMesh.faces = faces;
        pbMesh.ToMesh();
        pbMesh.Refresh();
        return pbMesh;
    }

    private void GenerateBridgeSupport(Vector3 position)
    {
        GameObject bridgeSupport = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bridgeSupport.transform.position = new Vector3(position.x, position.y - 2f, position.z);
        bridgeSupport.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
    }

    private Vector3 GetSplinePoint(float t)
    {
        int segment = Mathf.FloorToInt(t * (controlPoints.Count - 1));
        float localT = (t * (controlPoints.Count - 1)) - segment;

        Vector3 p0 = controlPoints[Mathf.Max(segment - 1, 0)];
        Vector3 p1 = controlPoints[segment];
        Vector3 p2 = controlPoints[Mathf.Min(segment + 1, controlPoints.Count - 1)];
        Vector3 p3 = controlPoints[Mathf.Min(segment + 2, controlPoints.Count - 1)];

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2 * p1) +
            (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t3
        );
    }
}
