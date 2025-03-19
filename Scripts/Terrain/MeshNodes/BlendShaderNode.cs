using UnityEngine;
using UnityEngine.ProBuilder;

public class BlendShaderNode : MeshNode
{
    public Texture2D texture1;
    public Texture2D texture2;
    public Texture2D texture3;
    public Texture2D texture4;
    public Texture2D splatMap; // Stores blending weights

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh inputMesh)
    {
        if (inputMesh == null)
        {
            Debug.LogError($"BlendShaderNode {name}: No input mesh!");
            return null;
        }

        Debug.Log($"🎨 Applying ProBuilder Blend Shader to {name}");

        // Ensure the GameObject has a MeshRenderer & Material
        MeshRenderer renderer = inputMesh.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = inputMesh.gameObject.AddComponent<MeshRenderer>();
        }

        // Assign ProBuilder Blend Shader Material
        Material blendMaterial = new Material(Shader.Find("ProBuilder/Standard Vertex Color"));
        renderer.sharedMaterial = blendMaterial;

        // Assign up to 4 textures
        if (texture1) blendMaterial.SetTexture("_MainTex", texture1);
        if (texture2) blendMaterial.SetTexture("_Texture2", texture2);
        if (texture3) blendMaterial.SetTexture("_Texture3", texture3);
        if (texture4) blendMaterial.SetTexture("_Texture4", texture4);

        // Assign a splat map (procedural blending)
        if (splatMap)
        {
            blendMaterial.SetTexture("_Control", splatMap);
        }
        else
        {
            Debug.LogWarning($"⚠ BlendShaderNode {name}: No splat map assigned! Default blending will be used.");
        }

        return inputMesh;
    }
}
