using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKController : NetworkBehaviour
{
    private RigBuilder rigBuilder;
    public Transform aimTarget; // ✅ Assign AimTarget in Inspector
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
        while (Camera.main == null) yield return null;

        cameraTransform = Camera.main.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("🚨 No Main Camera found!");
        }
    }

    void Update()
    {
        if (!isLocalPlayer || cameraTransform == null) return;

        // ✅ AimTarget is still used for shooting direction
        Vector3 newAimPosition = cameraTransform.position + cameraTransform.forward * 10f;

        // ✅ Stop arms from rotating but still allow upper body bending
        if (Vector3.Distance(syncedAimPosition, newAimPosition) > 0.01f)
        {
            aimTarget.position = newAimPosition;

            if (isServer)
            {
                syncedAimPosition = newAimPosition;
                RpcSyncAimPosition(newAimPosition);
            }
            else
            {
                CmdSyncAimPosition(newAimPosition);
            }
        }
    }

    [Command]
    void CmdSyncAimPosition(Vector3 newPosition)
    {
        syncedAimPosition = newPosition;
        RpcSyncAimPosition(newPosition);
    }

    [ClientRpc]
    void RpcSyncAimPosition(Vector3 newPosition)
    {
        if (!isLocalPlayer)
        {
            aimTarget.position = newPosition;
        }
    }

    void OnAimUpdated(Vector3 oldPosition, Vector3 newPosition)
    {
        if (!isLocalPlayer)
        {
            aimTarget.position = newPosition;
        }
    }
}
