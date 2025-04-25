using Mirror;
using UnityEngine;

public class PlayerVisibility : NetworkBehaviour
{
    public GameObject headMesh;  // Assign in Inspector
    public GameObject chestMesh; // Assign in Inspector
    public GameObject rLegMesh; // Assign in Inspector
    public GameObject lLegMesh; // Assign in Inspector
    public GameObject rArmMesh; // Assign in Inspector
    public GameObject lArmMesh; // Assign in Inspector
    public GameObject rHandMesh; // Assign in Inspector
    public GameObject lHandMesh; // Assign in Inspector
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
        if (rArmMesh) rArmMesh.SetActive(false);
        if (lArmMesh) lArmMesh.SetActive(false);
        if (rHandMesh) lHandMesh.SetActive(false);
        if (lHandMesh) rHandMesh.SetActive(false);
        Debug.Log("✅ Hiding Head & Chest for First-Person View!");
    }
}
