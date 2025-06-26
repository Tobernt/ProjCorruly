using Mirror;
using UnityEngine;
using System.Collections;

namespace CustomNamespace
{
    [AddComponentMenu("Custom/Networked Player Controller")]
    [RequireComponent(typeof(CharacterController))]
    public class CustomPlayerController : NetworkBehaviour
    {
        public int currentSeatIndex = -1; // Default value for no seat assigned
        [SyncVar(hook = nameof(OnMountedStateChanged))]
        public bool isMounted = false;
        private bool isCombatMode = false;
        public CharacterController characterController;
        private CapsuleCollider capsuleCollider;
        public float gravity;
        private WeaponController weaponController;
        public Vector3 velocity;
        private InventoryUI inventoryUI;
        private Animator animator;
        private Vector3 knockbackVelocity = Vector3.zero;
        public float knockbackDecayRate = 10f; // How quickly knockback fades
        [SyncVar(hook = nameof(OnHotbarChanged))]
        public string hotbarData = "";
        [Header("Local Components")]
        public Hotbar hotbar;
        public HotbarUI hotbarUI;
        public PlayerEquipmentTracker equipmentTracker;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            inventoryUI = FindObjectOfType<InventoryUI>();

            hotbarUI = GetComponentInChildren<HotbarUI>();
            hotbar = GetComponentInChildren<Hotbar>();
            equipmentTracker = GetComponent<PlayerEquipmentTracker>();
            equipmentTracker.enabled = true; // just in case it's disabled by default

            if (hotbar != null && hotbarUI != null)
            {
                for (int i = 0; i < hotbarUI.slotUIs.Count; i++)
                {
                    hotbarUI.slotUIs[i].Setup(i, null, hotbar); // Pass hotbar ref
                }

                hotbar.hotbarUI = hotbarUI;
                hotbarUI.RefreshUI();
            }
        }

        public void CmdEquipFromHotbar(string itemType, string itemID)
        {
            if (equipmentTracker == null)
            {
                Debug.LogError("❌ No equipmentTracker found on CmdEquipFromHotbar.");
                return;
            }

            equipmentTracker.UnequipItem(itemType);
            equipmentTracker.EquipItem(itemType, itemID);
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            inventoryUI = FindObjectOfType<InventoryUI>();
            weaponController = GetComponent<WeaponController>();
            if (animator != null)
            {
                int upperBodyLayerIndex = animator.GetLayerIndex("UpperBodyPassive");
                animator.SetLayerWeight(upperBodyLayerIndex, 1f); // Ensure layer is active
            }
            if (characterController == null)
            {
                Debug.LogError("CharacterController is missing on the player.");
            }

            if (capsuleCollider == null)
            {
                Debug.LogError("CapsuleCollider is missing on the player.");
            }
            isCombatMode = false;  // Ensure default mode is Passive
            animator.SetBool("CombatEnabled", isCombatMode);

            // Set proper layer weights at the start
            animator.SetLayerWeight(1, 1.0f); // Passive layer ON
            animator.SetLayerWeight(2, 0.0f); // Combat layer OFF

            // Ensure passive idle state is correctly set
            animator.Play("NormalIdle", 1);
        }
        private void OnHotbarChanged(string _, string newValue)
        {
            if (!isLocalPlayer || hotbar == null) return;
            hotbar.LoadFromSerialized(newValue);
        }
        public void CmdUpdateHotbar(string serializedData)
        {
            hotbarData = serializedData;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // Toggle Inventory UI
            if (Input.GetKeyDown(KeyCode.I) && !isMounted)
            {
                inventoryUI.ToggleInventory();
            }

            // ✅ Only disable camera and enable mouse when inventory is open
            if (inventoryUI != null && inventoryUI.IsInventoryOpen())
            {
                GetComponent<PlayerCamera>().enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                GetComponent<PlayerCamera>().enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // ✅ Keep handling movement and physics even when inventory is open
            HandleMovement();

            //if (isCombatMode && Input.GetButtonDown("Fire1"))
            //{
            //    Debug.Log("🟢 Fire Button Pressed!");
            //    TryShoot();
            //}

            // ✅ Handle mounting/unmounting cars
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!isMounted)
                {
                    CmdTryEnterCar();
                }
                else
                {
                    CmdExitCar();
                }
            }
        }
        public void CmdEquipItem(string itemType, string itemId)
        {
            // Perform the equip on the server
            PlayerEquipmentTracker.Instance.EquipItem(itemType, itemId);
            // Then tell all clients to also update visuals
            RpcEquipItem(itemType, itemId);
        }

        [ClientRpc]
        private void RpcEquipItem(string itemType, string itemId)
        {
            // On each client (including the owning client), apply the equip
            PlayerEquipmentTracker.Instance.EquipItem(itemType, itemId);
        }

        public void CmdSpawnPickup(string itemId, int quantity, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;

            var itemSO = ItemDatabaseSO.Instance.GetItemById(itemId);
            if (itemSO == null)
            {
                Debug.LogWarning($"❌ Invalid itemId passed to CmdSpawnPickup: {itemId}");
                return;
            }

            GameObject prefab = itemSO.pickupPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"❌ Item {itemId} has no pickup prefab assigned.");
                return;
            }

            GameObject pickup = Instantiate(prefab, position, rotation);
            PickupItem pickupItem = pickup.GetComponent<PickupItem>();
            if (pickupItem != null)
            {
                pickupItem.Initialize(itemId, quantity);
            }

            NetworkServer.Spawn(pickup);
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            // Toggle combat mode with a key press (example: 'X' key)
            if (Input.GetKeyDown(KeyCode.X))
            {
                isCombatMode = !isCombatMode;
                animator.SetBool("CombatEnabled", isCombatMode);

                if (weaponController != null)
                {
                    weaponController.isCombatMode = isCombatMode; // ✅ Tell weapon controller
                    if (weaponController.ammoText != null)
                        weaponController.ammoText.gameObject.SetActive(isCombatMode);

                    weaponController.ForceUpdateAmmoUI();
                }


                // Instantly transition to the correct upper body animation
                if (isCombatMode)
                {
                    animator.SetLayerWeight(2, 1.0f); // Enable combat layer
                    animator.SetLayerWeight(1, 0.0f); // Disable passive layer
                    animator.CrossFade("CombatIdle", 0.2f, 2); // Smooth transition in Combat Layer
                }
                else
                {
                    animator.SetLayerWeight(1, 1.0f); // Enable passive layer
                    animator.SetLayerWeight(2, 0.0f); // Disable combat layer
                    animator.CrossFade("NormalIdle", 0.2f, 1); // Smooth transition in Passive Layer
                }
            }

            // Check movement state
            bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
            bool isRunning = isMoving && Input.GetKey(KeyCode.LeftShift);

            // Set speed parameter for transitions
            float targetSpeed = isRunning ? 1.0f : (isMoving ? 0.5f : 0f);
            animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime); // Smooth transition

            // Get the current animation states
            AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);  // Base Layer
            AnimatorStateInfo upperCombatState = animator.GetCurrentAnimatorStateInfo(1); // UpperBodyCombat Layer
            AnimatorStateInfo upperPassiveState = animator.GetCurrentAnimatorStateInfo(2); // UpperBodyPassive Layer

            bool isInBaseIdle = baseState.IsName(isCombatMode ? "CombatIdle" : "NormalIdle");
            bool isInUpperCombatIdle = upperCombatState.IsName("CombatIdle");
            bool isInUpperPassiveIdle = upperPassiveState.IsName("NormalIdle");

            // Handle Upper Body Layers (Combat & Passive handled in the same logic)
            if (isCombatMode)
            {
                if (isMoving || isRunning)
                {
                    animator.SetLayerWeight(2, 1.0f); // Enable combat layer immediately
                    animator.SetLayerWeight(1, 0.0f); // Disable passive layer
                }
                else if (isInBaseIdle && isInUpperCombatIdle)
                {
                    animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(1), 0.0f, Time.deltaTime * 5f)); // Disable smoothly when idle
                }
            }
            else // If NOT in combat mode, assume passive
            {
                if (isMoving || isRunning)
                {
                    animator.SetLayerWeight(1, 1.0f); // Enable passive layer immediately
                    animator.SetLayerWeight(2, 0.0f); // Disable combat layer
                }
                else if (isInBaseIdle && isInUpperPassiveIdle)
                {
                    animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(2), 0.0f, Time.deltaTime * 5f)); // Disable smoothly when idle
                }
            }
        }

        private void OnMountedStateChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"🔹 isMounted changed from {oldValue} to {newValue}");
        }

        [ClientRpc]
        public void RpcUpdateMountedState(bool mounted)
        {
            isMounted = mounted;
            Debug.Log($"✅ RpcUpdateMountedState called. isMounted set to: {isMounted}");
        }

        public void TogglePhysics(bool enable)
        {
            RpcSetPhysics(enable);
            Debug.Log($"{gameObject.name}: Physics toggled to {(enable ? "enabled" : "disabled")}");

            if (!enable)
            {
                DisablePlayerControls();
            }
        }

        [ClientRpc]
        public void RpcSetPhysics(bool enable)
        {
            if (characterController != null)
            {
                characterController.enabled = enable;
            }

            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = enable;
            }

            Debug.Log($"{gameObject.name}: Physics set to {(enable ? "enabled" : "disabled")} on client.");
        }

        private void DisablePlayerControls()
        {
            characterController.enabled = false;
            GetComponentInChildren<PunchMechanic>().enabled = false;
            GetComponent<MountCarByDistanceWithSeats>().enabled = false;

            if (!isMounted)
            {
                GetComponent<PlayerCamera>().enabled = false;
            }
        }

        private void EnablePlayerControls()
        {
            characterController.enabled = true;
            GetComponent<MountCarByDistanceWithSeats>().enabled = true;
            GetComponentInChildren<PunchMechanic>().enabled = true;

            if (!isMounted)
            {
                GetComponent<PlayerCamera>().enabled = true;
            }
        }

        private void HandleMovement()
        {
            if (!characterController.enabled) return;

            bool isGrounded = characterController.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            Vector3 move = transform.right * horizontal + transform.forward * vertical;

            // Apply speed based on running state
            float currentSpeed = isRunning ? PlayerEquipmentTracker.Instance.Speed * 2f : PlayerEquipmentTracker.Instance.Speed; // 2x speed for running

            Vector3 totalMovement = (move * currentSpeed) + knockbackVelocity;
            characterController.Move(totalMovement * Time.deltaTime);

            // Decay knockback over time
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * knockbackDecayRate);


            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);

            // Update animation based on movement
            UpdateAnimation();
        }

        [TargetRpc]
        public void TargetApplyKnockback(NetworkConnection target, Vector3 direction, float force)
        {
            if (!isLocalPlayer) return;

            direction.Normalize();
            Vector3 knockback = direction * force;
            knockback.y = Mathf.Sqrt(force * 25f);

            knockbackVelocity = knockback;
            Debug.Log($"🌀 [TargetRpc] Knockback applied with lift: {knockback}");
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (!isLocalPlayer) return;

            direction.Normalize();

            Vector3 knockback = direction * force;
            knockback.y = Mathf.Sqrt(force * 25f); // realistic upward boost

            knockbackVelocity = knockback;

            Debug.Log($"🌀 Knockback applied with lift: {knockback}");
        }



        [ClientRpc]
        public void RpcDismountCar(Vector3 exitPosition)
        {
            Debug.Log($"test3 - RpcDismountCar() called on the client. Current isMounted: {isMounted}");

            isMounted = false;  // Ensure this is set to false
            transform.SetParent(null); // Detach from car
            transform.localEulerAngles = Vector3.zero; // Reset rotation
            transform.localScale = Vector3.one; // Reset scale

            // Move the player to the correct exit position
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = exitPosition;
                characterController.enabled = true;
            }

            Debug.Log($"✅ Player exited to {exitPosition}. isMounted: {isMounted}");

            // ✅ Re-enable physics after a short delay
            StartCoroutine(EnablePhysicsAfterDelay(0.1f));
        }

        private IEnumerator EnablePhysicsAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            RpcSetPhysics(true);
        }

        // ✅ The player sends a command to request entry into a car
        [Command(requiresAuthority = false)]
        private void CmdTryEnterCar()
        {
            GameObject closestCar = FindClosestCar();
            if (closestCar != null)
            {
                MountableCar car = closestCar.GetComponent<MountableCar>();
                if (car != null)
                {
                    int seatIndex = car.AssignSeat(this);
                    if (seatIndex >= 0)
                    {
                        RpcMountCar(car.gameObject, seatIndex);
                    }
                }
            }
        }

        // ✅ The player sends a command to exit the car
        [Command(requiresAuthority = false)]
        private void CmdExitCar()
        {
            if (isMounted)
            {
                MountableCar car = GetMountedCar();
                if (car != null)
                {
                    car.FreeSeat(currentSeatIndex, this);
                }
                RpcExitCar();
            }
        }

        [ClientRpc]
        private void RpcMountCar(GameObject carObject, int seatIndex)
        {
            isMounted = true;
            MountableCar car = carObject.GetComponent<MountableCar>();
            Transform seat = car.seats[seatIndex];

            transform.SetParent(seat);
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;

            Debug.Log($"Mounted car: {carObject.name} at seat {seat.name}");
        }

        [ClientRpc]
        private void RpcExitCar()
        {
            isMounted = false;
            transform.SetParent(null);
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
            Debug.Log("Left the car.");
        }

        private GameObject FindClosestCar()
        {
            GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");
            GameObject closestCar = null;
            float minDistance = 3f;

            foreach (GameObject car in cars)
            {
                float distance = Vector3.Distance(transform.position, car.transform.position);
                if (distance < minDistance)
                {
                    closestCar = car;
                    minDistance = distance;
                }
            }

            return closestCar;
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestCarAuthority(NetworkIdentity carIdentity)
        {
            if (carIdentity == null)
            {
                Debug.LogError($"CmdRequestCarAuthority called on {name} with a null carIdentity.", gameObject);
                return;
            }

            if (carIdentity.connectionToClient == null)
            {
                carIdentity.AssignClientAuthority(connectionToClient);
                Debug.Log($"✅ Authority assigned to player {name} for car {carIdentity.name}");
            }
            else
            {
                Debug.LogWarning($"🚫 Car {carIdentity.name} already has an owner: {carIdentity.connectionToClient.address}");
            }
        }
        public void EnableCombatMode()
        {
            isCombatMode = true;
            if (animator != null)
            {
                animator.SetBool("CombatEnabled", true);
                animator.SetLayerWeight(2, 1.0f); // Combat layer ON
                animator.SetLayerWeight(1, 0.0f); // Passive layer OFF
                animator.CrossFade("CombatIdle", 0.2f, 2);
            }

            if (weaponController != null)
            {
                weaponController.isCombatMode = true;
                if (weaponController.ammoText != null)
                    weaponController.ammoText.gameObject.SetActive(true);
                weaponController.ForceUpdateAmmoUI();
            }

            Debug.Log("⚔️ Entered combat mode due to weapon equip.");
        }

        [Command(requiresAuthority = false)]
        public void CmdRemoveCarAuthority(NetworkIdentity carIdentity)
        {
            if (carIdentity == null)
            {
                Debug.LogError($"CmdRemoveCarAuthority called on {name} with a null carIdentity.", gameObject);
                return;
            }

            if (carIdentity.connectionToClient == connectionToClient)
            {
                carIdentity.RemoveClientAuthority();
                Debug.Log($"❌ Authority removed from player {name} for car {carIdentity.name}");
            }
            else
            {
                Debug.LogWarning($"🚫 Player {name} tried to remove authority but does not own {carIdentity.name}");
            }
        }
        private MountableCar GetMountedCar()
        {
            if (isMounted)
            {
                return transform.parent?.GetComponentInParent<MountableCar>();
            }
            return null;
        }
    }
}
