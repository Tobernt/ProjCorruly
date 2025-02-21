using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKController : NetworkBehaviour
{
    private RigBuilder rigBuilder;
    public Transform aimTarget; // Assign AimTarget in Inspector
    private Transform cameraTransform;

    [SyncVar(hook = nameof(OnAimUpdated))]
    private Vector3 syncedAimPosition;

    public override void OnStartLocalPlayer()
    {
        rigBuilder = GetComponentInChildren<RigBuilder>();
        StartCoroutine(InitializeCamera()); // ✅ Delayed initialization
    }

    private System.Collections.IEnumerator InitializeCamera()
    {
        // Wait until Camera.main is available
        while (Camera.main == null)
        {
            yield return null;
        }

        cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("🚨 No Main Camera found! Make sure the player camera is set correctly.");
        }
    }

    void Update()
    {
        if (!isLocalPlayer || cameraTransform == null) return; // ✅ Prevent null reference

        // Calculate new AimTarget position based on camera
        Vector3 newAimPosition = cameraTransform.position + cameraTransform.forward * 10f;

        // Only update if there's a significant change
        if (Vector3.Distance(syncedAimPosition, newAimPosition) > 0.01f)
        {
            aimTarget.position = newAimPosition;

            if (isServer)
            {
                syncedAimPosition = newAimPosition; // Directly update on the server
                RpcSyncAimPosition(newAimPosition); // Send to all clients
            }
            else
            {
                CmdSyncAimPosition(newAimPosition); // Send to server
            }
        }
    }

    // Clients send their aim position to the server
    [Command]
    void CmdSyncAimPosition(Vector3 newPosition)
    {
        syncedAimPosition = newPosition; // Update on server
        RpcSyncAimPosition(newPosition); // Broadcast to clients
    }

    // Sync aim position for all clients
    [ClientRpc]
    void RpcSyncAimPosition(Vector3 newPosition)
    {
        if (!isLocalPlayer) // Remote clients update their aim position
        {
            aimTarget.position = newPosition;
        }
    }

    // SyncVar Hook - Ensures remote players update smoothly
    void OnAimUpdated(Vector3 oldPosition, Vector3 newPosition)
    {
        if (!isLocalPlayer) // Only remote players adjust their aim
        {
            aimTarget.position = newPosition;
        }
    }
}
