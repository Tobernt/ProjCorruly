using CustomNamespace;
using UnityEngine;

public class SpeedBoostEffect : MonoBehaviour
{
    private CustomPlayerController playerController;
    private float runMultiplier = 1.5f; // Boosts only running speed

    private void Start()
    {
        playerController = GetComponentInParent<CustomPlayerController>();

        if (playerController == null)
        {
            Debug.LogError("❌ SpeedBoostEffect: PlayerController not found!");
            return;
        }

        Debug.Log($"✅ SpeedBoostEffect applied! Running speed x{runMultiplier}");
        PlayerEquipmentTracker.Instance.ApplyRunMultiplier(runMultiplier);
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            Debug.Log($"❌ SpeedBoostEffect removed! Reverting running speed.");
            PlayerEquipmentTracker.Instance.RemoveRunMultiplier(runMultiplier);
        }
    }
}
