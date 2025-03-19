using CustomNamespace;
using UnityEngine;

public class DoubleJumpEffect : MonoBehaviour
{
    private CustomPlayerController playerController;
    private DoubleJumpHandler jumpHandler;
    private int extraJumps = 1; // Each ring grants 1 extra jump

    private void Start()
    {
        playerController = GetComponentInParent<CustomPlayerController>();
        jumpHandler = playerController.GetComponent<DoubleJumpHandler>();

        if (playerController == null || jumpHandler == null)
        {
            Debug.LogError("❌ DoubleJumpEffect: Could not find required components on the player!");
            return;
        }

        Debug.Log($"✅ DoubleJumpEffect applied! Extra Jumps: {extraJumps}");
        jumpHandler.IncreaseMaxJumps(extraJumps);
    }

    private void OnDestroy()
    {
        if (playerController != null && jumpHandler != null)
        {
            Debug.Log($"❌ DoubleJumpEffect removed! Removing {extraJumps} extra jumps.");
            jumpHandler.IncreaseMaxJumps(-extraJumps);
        }
    }
}
