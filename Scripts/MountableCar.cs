using CustomNamespace;
using Mirror;
using UnityEngine;

[AddComponentMenu("Custom/Mountable Car")]
public class MountableCar : NetworkBehaviour
{
    [Header("Car Settings")]
    public Transform[] seats;
    private bool[] seatOccupied;
    [Header("Flip Detection Settings")]
    public float flipThreshold = 0.7f;
    public float flipTime = 6f;
    private float flipTimer = 0f;
    [SyncVar] public PhysicsScene physicsScene;
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private float accelerationForce = 500f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float turnTorque = 50f; // 🔹 Reduced turning force
    [SerializeField] private float minTurnFactor = 0.2f; // 🔹 Less turn at low speeds
    [SerializeField] private float dragFactor = 0.98f; // 🔹 Simulates friction/drag
    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastDistance = 2.0f; // ✅ Editable Raycast Distance
    [SerializeField] private float raycastOffsetY = 0.5f;  // ✅ Editable Start Offset
    [SyncVar] private bool isGrounded;

    [SyncVar(hook = nameof(OnDriverChanged))] private NetworkIdentity currentDriver;

    private NetworkIdentity carIdentity;

    private void Awake()
    {
        seatOccupied = new bool[seats.Length];
        carIdentity = GetComponent<NetworkIdentity>();
    }

    private void Update()
    {
        CheckFlipStatus();
    }

    private void CheckFlipStatus()
    {
        // Check if the car is flipped based on its upward vector
        if (Vector3.Dot(transform.up, Vector3.up) < flipThreshold)
        {
            // If flipped, increase the flip timer
            flipTimer += Time.deltaTime;

            if (flipTimer >= flipTime)
            {
                CmdFlipCar();
                flipTimer = 0f; // Reset the timer after flipping
            }
        }
        else
        {
            // Reset the timer if the car is not flipped
            flipTimer = 0f;
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdFlipCar()
    {
        RpcFlipCar();
    }

    [ClientRpc]
    private void RpcFlipCar()
    {
        // Reset the car's rotation to upright
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        Debug.Log("Car flipped back to upright position.");
    }

    public int AssignSeat(CustomPlayerController player)
    {        
        physicsScene = gameObject.scene.GetPhysicsScene();
        for (int i = 0; i < seats.Length; i++)
        {
            if (!seatOccupied[i])
            {
                seatOccupied[i] = true;
                player.currentSeatIndex = i;
                player.isMounted = true;

                // Force the client to update physics state
                player.RpcSetPhysics(false);

                if (i == 0) // Seat 1 (index 0) is the driver's seat
                {
                    CmdAssignAuthority(player.netIdentity);
                    currentDriver = player.netIdentity;
                    Debug.Log($"Driver assigned: {player.name}");
                }

                RpcMoveCarUp(); // Apply the upward offset when mounting

                return i; // Return the index of the assigned seat
            }
        }
        Debug.Log("No available seats.");
        return -1; // No seats available
    }

    public void FreeSeat(int seatIndex, CustomPlayerController player)
    {
        if (seatIndex >= 0 && seatIndex < seatOccupied.Length)
        {
            seatOccupied[seatIndex] = false;

            // Force the client to update physics state
            player.RpcSetPhysics(true);

            if (seatIndex == 0) // Driver seat
            {
                RpcNotifyDriverRemoved(player.netIdentity, this.netIdentity);
                currentDriver = null;
                Debug.Log($"Driver removed: {player.name}");
            }
        }
    }


    [ClientRpc]
    private void RpcMoveCarUp()
    {
        if (!isServer)
        {
            transform.position += Vector3.up * 1f;
        }
    }

    private void OnDriverChanged(NetworkIdentity oldDriver, NetworkIdentity newDriver)
    {
        Debug.Log($"Driver changed from {oldDriver?.name} to {newDriver?.name}");
    }

    private void FixedUpdate()
    {
        if (isOwned)
        {
            PerformGroundCheck();
            HandleDriving();
        }
    }

    private void PerformGroundCheck()
    {
        if (!physicsScene.IsValid())
        {
            Debug.LogError("❌ Invalid physics scene! Ground check skipped.");
            return;
        }

        Vector3 startPosition = transform.position + Vector3.up * raycastOffsetY;
        float sphereRadius = 0.5f; // ✅ Adjustable SphereCast radius
        float castDistance = raycastDistance;

        RaycastHit hit;
        bool grounded = physicsScene.SphereCast(startPosition, sphereRadius, Vector3.down, out hit, castDistance, groundLayer);

        isGrounded = grounded;

        Debug.Log(isGrounded
            ? $"✅ Car Ground Detected! Hit: {hit.collider?.name}, Distance: {hit.distance}"
            : "❌ No ground detected!");

        // ✅ Draw debug sphere
        Debug.DrawRay(startPosition, Vector3.down * castDistance, isGrounded ? Color.green : Color.red, 0.1f);
    }

    private void HandleDriving()
    {
        if (!isGrounded) return; // Only allow driving if grounded and has authority

        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        if (moveInput != 0)
        {
            Vector3 force = transform.forward * moveInput * accelerationForce;
            carRigidbody.AddForce(force, ForceMode.Acceleration);
        }

        float speedFactor = Mathf.Clamp(carRigidbody.velocity.magnitude / maxSpeed, minTurnFactor, 1f);
        float adjustedTurnTorque = turnInput * turnTorque * speedFactor;

        if (Mathf.Abs(turnInput) > 0.1f)
        {
            carRigidbody.AddTorque(Vector3.up * adjustedTurnTorque, ForceMode.Acceleration);
        }

        carRigidbody.velocity *= dragFactor; // Apply slight drag to stabilize movement
    }

    [Command(requiresAuthority = false)] // Allow the server to always execute this command
    private void CmdAssignAuthority(NetworkIdentity playerIdentity)
    {
        if (carIdentity.connectionToClient != null)
        {
            carIdentity.RemoveClientAuthority();
        }
        carIdentity.AssignClientAuthority(playerIdentity.connectionToClient);
        Debug.Log($"Authority assigned to: {playerIdentity.connectionToClient.address}");
    }

    [ClientRpc]
    private void RpcNotifyDriverRemoved(NetworkIdentity playerIdentity, NetworkIdentity carIdentity)
    {
        if (playerIdentity.isLocalPlayer) // Only the local player should execute this
        {
            Debug.Log($"🔹 Local player removing car authority.");
            playerIdentity.GetComponent<CustomPlayerController>().CmdRemoveCarAuthority(carIdentity);
        }
    }
}