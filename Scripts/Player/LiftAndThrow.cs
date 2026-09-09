using Mirror;
using UnityEngine;

public class LiftAndThrowMechanic : NetworkBehaviour
{
    [Header("Throw Settings")]
    public float throwForce = 15f;
    public float maxHoldDistance = 3f;
    public float objectFollowStrength = 10f;

    [Header("Lift Settings")]
    public Transform holdPosition;
    private GameObject liftedObject;

    private Camera playerCamera;

    private void Start()
    {
        if (!isLocalPlayer) return;

        playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogError("[LiftAndThrow] Camera not found!");

        if (holdPosition == null)
            Debug.LogError("[LiftAndThrow] HoldPosition not assigned!");
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Handle object follow movement
        if (liftedObject != null)
        {
            MoveLiftedObject();

            if (liftedObject != null && playerCamera != null)
            {
                RotateLiftedObjectToAim();
            }

            if (Vector3.Distance(liftedObject.transform.position, holdPosition.position) > maxHoldDistance)
            {
                DropObject();
            }
        }

        // Throw with left click
        if (Input.GetMouseButtonDown(0) && liftedObject != null)
        {
            ThrowObject();
        }

        // Lift / Drop with right click
        if (Input.GetMouseButtonDown(1))
        {
            if (liftedObject == null)
                TryLiftObject();
            else
                DropObject();
        }
    }
    private void RotateLiftedObjectToAim()
    {
        if (liftedObject == null) return;

        // Get the direction the camera is looking
        Vector3 targetDirection = playerCamera.transform.forward;

        // Optional: only rotate on Y axis
        targetDirection.y = 0;

        if (targetDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            liftedObject.transform.rotation = Quaternion.Slerp(
                liftedObject.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f // Smooth speed, adjust as needed
            );
        }
    }

    private void TryLiftObject()
    {
        GameObject target = FindClosestLiftable();
        if (target != null)
        {
            CmdRequestLift(target);
        }
    }

    private GameObject FindClosestLiftable()
    {
        GameObject[] liftables = GameObject.FindGameObjectsWithTag("Liftable");
        GameObject closest = null;
        float closestDist = maxHoldDistance;

        foreach (GameObject obj in liftables)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < closestDist)
            {
                closest = obj;
                closestDist = dist;
            }
        }

        return closest;
    }

    private void MoveLiftedObject()
    {
        if (liftedObject == null) return;

        Rigidbody rb = liftedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (holdPosition.position - liftedObject.transform.position);
            rb.velocity = direction * objectFollowStrength;
        }
    }

    private void DropObject()
    {
        if (liftedObject == null) return;

        CmdRequestDrop(liftedObject);
        liftedObject = null;
    }

    private void ThrowObject()
    {
        if (liftedObject == null) return;

        Vector3 throwDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        CmdThrowLiftedObject(liftedObject, throwDirection);
        liftedObject = null;
    }

    // === Server Commands ===

    [Command]
    private void CmdRequestLift(GameObject target)
    {
        if (target == null || liftedObject != null) return;

        NetworkIdentity identity = target.GetComponent<NetworkIdentity>();
        if (identity != null && identity.connectionToClient == null)
        {
            identity.AssignClientAuthority(connectionToClient);
            RpcLiftObject(target);
        }
    }

    [Command]
    private void CmdRequestDrop(GameObject target)
    {
        if (target == null) return;

        NetworkIdentity identity = target.GetComponent<NetworkIdentity>();
        if (identity != null && identity.connectionToClient == connectionToClient)
        {
            identity.RemoveClientAuthority();
            RpcDropObject(target);
        }
    }

    [Command]
    private void CmdThrowLiftedObject(GameObject target, Vector3 direction)
    {
        if (target == null) return;

        NetworkIdentity identity = target.GetComponent<NetworkIdentity>();
        if (identity != null && identity.connectionToClient == connectionToClient)
        {
            identity.RemoveClientAuthority();
            RpcThrowObject(target, direction);
        }
    }

    // === Client RPCs ===

    [ClientRpc]
    private void RpcLiftObject(GameObject target)
    {
        liftedObject = target;

        Rigidbody rb = liftedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.drag = 10f;
        }
    }

    [ClientRpc]
    private void RpcDropObject(GameObject target)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.drag = 1f;
        }

        if (target == liftedObject)
        {
            liftedObject = null;
        }
    }

    [ClientRpc]
    private void RpcThrowObject(GameObject target, Vector3 direction)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.drag = 1f;
            rb.AddForce(direction.normalized * throwForce, ForceMode.Impulse);
        }
    }
}
