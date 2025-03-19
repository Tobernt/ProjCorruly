using Mirror;
using UnityEngine;

public class PlayerVisibility : NetworkBehaviour
{
    public GameObject headMesh;  // Assign in Inspector
    public GameObject chestMesh; // Assign in Inspector
    public GameObject rLegMesh; // Assign in Inspector
    public GameObject lLegMesh; // Assign in Inspector

    public override void OnStartLocalPlayer()
    {
        Debug.Log("test");
        HideFirstPersonObstructions();
    }

    void HideFirstPersonObstructions()
    {
        Debug.Log("test2");
        if (headMesh) headMesh.SetActive(false);
        if (chestMesh) chestMesh.SetActive(false);
        if (rLegMesh) rLegMesh.SetActive(false);
        if (lLegMesh) lLegMesh.SetActive(false);
        Debug.Log("✅ Hiding Head & Chest for First-Person View!");
    }
}
