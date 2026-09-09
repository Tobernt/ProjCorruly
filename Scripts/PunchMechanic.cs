using Mirror;
using UnityEngine;

public class PunchMechanic : NetworkBehaviour
{
    [Header("Punch Settings")]
    public float punchForce = 10f;        // Force applied to objects
    public float punchDamage = 10;       // Damage dealt by the punch
    public float punchRange = 3f;        // Maximum distance for the punch
    public float punchAngle = 45f;       // Cone angle for detecting punchable objects
    public float punchCooldown = 0.5f;   // Time between punches
    public LayerMask punchableLayers;    // Layers that can be punched

    [Header("Lift Settings")]
    public Transform holdPosition;       // Position where the dummy will be held
    private GameObject liftedObject;     // The currently lifted object
    [Header("Lift Settings")]
    [SerializeField] private float maxHoldDistance = 3f; // Drop if object moves too far
    [SerializeField] private float objectFollowStrength = 10f; // Controls how tightly it follows

    private float lastPunchTime = -1f;
    private Animator animator;

    // Reference to the camera for determining look direction
    private Camera playerCamera;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PunchMechanic] Animator component not found!");
        }

        playerCamera = Camera.main; // Get the main camera
        if (playerCamera == null)
        {
            Debug.LogError("[PunchMechanic] Camera not found!");
        }

        if (holdPosition == null)
        {
            Debug.LogError("[PunchMechanic] HoldPosition is not assigned!");
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Lifted object logic
        if (liftedObject != null)
        {
            MoveLiftedObject();

            // Check if it should be dropped
            if (Vector3.Distance(liftedObject.transform.position, holdPosition.position) > maxHoldDistance)
            {
                Debug.Log("[PunchMechanic] Lifted object moved too far. Dropping...");
                DropObject();
            }
        }

        // Punch
        if (Input.GetMouseButtonDown(0) && Time.time >= lastPunchTime + punchCooldown)
        {
            PerformPunch();
            lastPunchTime = Time.time;
        }

        // Lift/Drop
        if (Input.GetMouseButtonDown(1))
        {
            if (liftedObject == null)
            {
                TryLiftObject();
            }
            else
            {
                DropObject();
            }
        }
    }
    private void MoveLiftedObject()
    {
        if (liftedObject == null) return;

        // Apply smooth movement instead of parenting
        Vector3 targetPosition = holdPosition.position;
        Vector3 moveDirection = (targetPosition - liftedObject.transform.position);

        Rigidbody rb = liftedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = moveDirection * objectFollowStrength; // Smooth following
        }
    }
    private void PerformPunch()
    {
        Debug.Log("[PunchMechanic] Attempting to punch...");

        // Trigger the punch animation immediately on the client
        if (animator != null)
        {
            animator.SetBool("IsPunching", true);
            Invoke(nameof(ResetPunchAnimation), 0.2f); // Reset after 0.2 seconds
        }

        // Predictively show effects on the client (if applicable)
        Vector3 punchDirection = GetPunchDirection();

        if (liftedObject != null)
        {
            Debug.Log($"[PunchMechanic] Punching lifted object: {liftedObject.name}");
            CmdHandlePunch(liftedObject, punchDirection); // Server processes punch logic
            DropObject(); // Drop the object after punching it
        }
        else
        {
            GameObject target = FindClosestPunchable();
            if (target != null)
            {
                Debug.Log($"[PunchMechanic] Punched: {target.name}");
                CmdHandlePunch(target, punchDirection); // Server processes punch logic
            }
            else
            {
                Debug.Log("[PunchMechanic] Punch missed! No objects within range and angle.");
            }
        }
    }

    private void ResetPunchAnimation()
    {
        CmdSetIsPunching(false);
    }

    private Vector3 GetPunchDirection()
    {
        return playerCamera != null ? playerCamera.transform.forward : transform.forward;
    }

    private GameObject FindClosestPunchable()
    {
        GameObject[] punchableObjects = GameObject.FindGameObjectsWithTag("Punchable");
        GameObject closestObject = null;
        float closestDistance = punchRange;

        foreach (GameObject obj in punchableObjects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance <= punchRange)
            {
                Vector3 directionToTarget = (obj.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget <= punchAngle && distance < closestDistance)
                {
                    closestObject = obj;
                    closestDistance = distance;
                }
            }
        }

        return closestObject;
    }

    [Command]
    private void CmdHandlePunch(GameObject targetObject, Vector3 direction)
    {
        if (targetObject == null) return;

        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(direction * punchForce, ForceMode.Impulse);
        }

        Health health = targetObject.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage((int)punchDamage);
        }

        RpcPunchEffect(targetObject, direction);
    }
    private void TryLiftObject()
    {
        if (liftedObject != null)
        {
            Debug.Log("[PunchMechanic] Already holding an object. Cannot lift another.");
            return;
        }

        GameObject target = FindClosestPunchable();
        if (target != null)
        {
            Debug.Log($"[PunchMechanic] Lifting object: {target.name}");
            CmdRequestLiftObject(target);
        }
        else
        {
            Debug.Log("[PunchMechanic] No objects to lift within range.");
        }
    }

    [Command]
    private void CmdRequestLiftObject(GameObject targetObject)
    {
        if (targetObject == null || liftedObject != null) return;

        NetworkIdentity targetNetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetNetIdentity != null && targetNetIdentity.connectionToClient == null)
        {
            targetNetIdentity.AssignClientAuthority(connectionToClient);
            RpcLiftObject(targetObject);
        }
    }

    [ClientRpc]
    private void RpcLiftObject(GameObject targetObject)
    {
        if (targetObject == null) return;

        liftedObject = targetObject;
        Rigidbody rb = liftedObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;  // Disable gravity while lifting
            rb.drag = 10f;          // Increase drag for smoother control
        }
    }
    private void DropObject()
    {
        if (liftedObject != null)
        {
            Debug.Log($"[PunchMechanic] Dropping object: {liftedObject.name}");
            CmdRequestDropObject(liftedObject);
            liftedObject = null;
        }
    }

    [Command]
    private void CmdRequestDropObject(GameObject targetObject)
    {
        if (targetObject == null) return;

        NetworkIdentity targetNetIdentity = targetObject.GetComponent<NetworkIdentity>();
        if (targetNetIdentity != null && targetNetIdentity.connectionToClient == connectionToClient)
        {
            targetNetIdentity.RemoveClientAuthority();
            RpcDropObject(targetObject);
        }
    }

    [ClientRpc]
    private void RpcDropObject(GameObject targetObject)
    {
        if (targetObject != null)
        {
            Rigidbody rb = targetObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;  // Re-enable gravity on drop
                rb.drag = 1f;          // Reset drag to normal
            }

            if (targetObject == liftedObject)
            {
                liftedObject = null;
            }
        }
    }

    [Command]
    private void CmdSetIsPunching(bool isPunching)
    {
        RpcSetIsPunching(isPunching);
    }

    [ClientRpc]
    private void RpcSetIsPunching(bool isPunching)
    {
        if (animator != null)
        {
            animator.SetBool("IsPunching", isPunching);
        }
    }

    [ClientRpc]
    private void RpcPunchEffect(GameObject hitObject, Vector3 direction)
    {
        if (hitObject == null) return;

        Rigidbody rb = hitObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(direction * punchForce, ForceMode.Impulse);
        }

        Debug.Log($"[Client] Punch effect on: {hitObject.name}");
    }
}
