using UnityEngine;
using System.Collections.Generic;

public class GrassInteractor : MonoBehaviour
{
    public float detectionRadius = 10f; // how far it looks for players/enemies
    public int maxInteractionPoints = 4; // supports up to 4 in shader

    private void Update()
    {
        Vector4[] positions = new Vector4[4];
        List<Transform> closeTargets = new List<Transform>();

        // Combine all targets from both tags
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Punchable");

        Vector3 center = transform.position;

        foreach (var obj in players)
        {
            if (Vector3.Distance(center, obj.transform.position) <= detectionRadius)
                closeTargets.Add(obj.transform);
        }

        foreach (var obj in enemies)
        {
            if (Vector3.Distance(center, obj.transform.position) <= detectionRadius)
                closeTargets.Add(obj.transform);
        }

        // Sort by distance and send the closest ones
        closeTargets.Sort((a, b) =>
            Vector3.Distance(center, a.position).CompareTo(Vector3.Distance(center, b.position)));

        for (int i = 0; i < positions.Length; i++)
        {
            if (i < closeTargets.Count)
                positions[i] = closeTargets[i].position;
            else
                positions[i] = new Vector4(9999, 9999, 9999, 0); // out of range
        }

        Shader.SetGlobalVectorArray("_InteractionPos", positions);
    }
}
