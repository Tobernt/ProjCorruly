using CustomNamespace;
using Mirror;
using UnityEngine;
using System.Collections;

public class MountCarByDistanceWithSeats : NetworkBehaviour
{
    public float interactionRange = 3f;
    [SyncVar] public bool isMounted = false;
    [SyncVar] private MountableCar currentCar;

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.E) && !isMounted)
        {
            GameObject carObject = FindClosestCar();
            if (carObject != null)
            {
                currentCar = carObject.GetComponent<MountableCar>();
                if (currentCar != null)
                {
                    CmdRequestSeat(carObject);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.E) && isMounted)
        {
            CmdLeaveCar();
        }
    }

    private GameObject FindClosestCar()
    {
        GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");
        GameObject closestCar = null;
        float minDistance = interactionRange;

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
    private void CmdRequestSeat(GameObject carObject)
    {
        if (carObject == null)
        {
            Debug.LogError("❌ CmdRequestSeat failed - carObject is NULL!", gameObject);
            return;
        }

        MountableCar car = carObject.GetComponent<MountableCar>();
        if (car != null)
        {
            CustomPlayerController player = GetComponent<CustomPlayerController>();
            int seatIndex = car.AssignSeat(player);

            if (seatIndex >= 0)
            {
                currentCar = car;
                RpcUpdateMountedState(true);  // ✅ Force `isMounted = true` to sync across network
                RpcMountCar(carObject, seatIndex);
            }
            else
            {
                Debug.LogWarning($"🚫 CmdRequestSeat: No available seats in {car.name}");
            }
        }
    }
    [ClientRpc]
    public void RpcUpdateMountedState(bool mounted)
    {
        isMounted = mounted;
        Debug.Log($"✅ RpcUpdateMountedState called. isMounted set to: {isMounted}");
    }


    [ClientRpc]
    private void RpcMountCar(GameObject carObject, int seatIndex)
    {
        isMounted = true;
        currentCar = carObject.GetComponent<MountableCar>();
        Transform seat = currentCar.seats[seatIndex]; // Resolve seat locally
        transform.SetParent(seat);
        transform.localPosition = Vector3.zero;

        // Reset the player's rotation to (0, 0, 0) when mounting
        transform.localEulerAngles = Vector3.zero;

        CustomPlayerController player = GetComponent<CustomPlayerController>();
        if (player != null)
        {
            player.isMounted = true; // Ensure isMounted is synced across clients
        }

        Debug.Log($"Mounted car: {carObject.name} at seat {seat.name}");
    }

    [Command(requiresAuthority = false)]
    private void CmdLeaveCar()
    {
        Debug.Log("test1 - CmdLeaveCar called");

        if (currentCar == null)
        {
            Debug.LogError("❌ CmdLeaveCar failed - currentCar is NULL on the server!", gameObject);
            return;
        }

        Debug.Log($"✅ Leaving car: {currentCar.name}");

        CustomPlayerController player = GetComponent<CustomPlayerController>();

        // ✅ Ensure `isMounted` is set to false
        player.isMounted = false;
        RpcUpdateMountedState(false); // 🔥 Call it once here
        Debug.Log($"🔹 isMounted set to: {player.isMounted} on server");

        // Get seat exit position
        int seatIndex = player.currentSeatIndex;
        if (seatIndex >= 0 && seatIndex < currentCar.seats.Length)
        {
            Vector3 exitPosition = currentCar.seats[seatIndex].position + (currentCar.seats[seatIndex].forward * 2f) + (Vector3.up * 1.5f);
            player.RpcDismountCar(exitPosition);
        }
        else
        {
            Debug.LogWarning($"⚠️ CmdLeaveCar: Invalid seat index {seatIndex} on {currentCar.name}");
        }

        // Notify the car that the seat is free
        currentCar.FreeSeat(player.currentSeatIndex, player);

        // Reset seat index on the player
        player.currentSeatIndex = -1;
        player.isMounted = false;
        RpcUpdateMountedState(false); // 🔥 Call it AGAIN for extra sync safety

        // ✅ Ensure `currentCar` is reset properly
        currentCar = null;
        Debug.Log($"🚗 Player successfully left car. isMounted: {player.isMounted}, currentCar: {currentCar}");
    }

    private IEnumerator ClearCarReferenceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentCar = null;
    }

    [ClientRpc]
    private void RpcDismountCar(Vector3 exitPosition)
    {
        Debug.Log("test3 - RpcDismountCar() called on the client.");

        isMounted = false;
        transform.SetParent(null); // Detach from the car
        transform.localEulerAngles = Vector3.zero; // Reset rotation
        transform.localScale = Vector3.one; // Reset scale

        // ✅ Move the player to the calculated exit position
        CustomPlayerController player = GetComponent<CustomPlayerController>();
        if (player != null)
        {
            player.characterController.enabled = false;
            transform.position = exitPosition;
            player.characterController.enabled = true;
        }

        Debug.Log($"✅ Player exited to {exitPosition}");

        // ✅ Re-enable physics
        if (player != null)
        {
            Debug.Log("test4 - Player found, setting isMounted to false and re-enabling physics.");
            StartCoroutine(EnablePhysicsAfterDelay(player, 0.1f));
        }
    }

    private IEnumerator EnablePhysicsAfterDelay(CustomPlayerController player, float delay)
    {
        yield return new WaitForSeconds(delay);
        player.RpcSetPhysics(true);
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

}
