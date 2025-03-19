using UnityEngine;
using UnityEngine.ProBuilder;

public abstract class MeshNode : GraphNode
{
    public abstract ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh);
    [System.NonSerialized]
    public ProBuilderMesh GeneratedMesh;

}
