using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ProceduralMeshGeneration
{
    public class MeshGenerator
    {
        private GraphProcessor _graphProcessor;
        private Mesh _cachedMesh; // Cache to prevent unnecessary recalculations

        public MeshGenerator(GraphProcessor graphProcessor)
        {
            _graphProcessor = graphProcessor;
        }

        public Mesh GenerateUnityMesh()
        {
            List<ProBuilderMesh> pbMeshes = _graphProcessor.ExecuteGraph(); // Get list of tiles
            if (pbMeshes == null || pbMeshes.Count == 0) return null;

            Mesh combinedMesh = new Mesh();
            CombineInstance[] combine = new CombineInstance[pbMeshes.Count];

            for (int i = 0; i < pbMeshes.Count; i++)
            {
                Mesh tempMesh = new Mesh();
                pbMeshes[i].ToMesh();
                pbMeshes[i].Refresh();
                pbMeshes[i].GetComponent<MeshFilter>().mesh = tempMesh;

                combine[i].mesh = tempMesh;
                combine[i].transform = pbMeshes[i].transform.localToWorldMatrix;
            }

            combinedMesh.CombineMeshes(combine);
            return combinedMesh;
        }
    }
}
