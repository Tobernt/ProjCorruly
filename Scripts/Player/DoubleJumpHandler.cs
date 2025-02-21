using CustomNamespace;
using UnityEngine;

public class DoubleJumpHandler : MonoBehaviour
{
    private CustomPlayerController playerController;
    private int currentJumpCount = 0;
    private int maxJumps = 1; // Default base jump

    private void Start()
    {
        playerController = GetComponent<CustomPlayerController>();
        if (playerController == null)
        {
            Debug.LogError("❌ DoubleJumpHandler: PlayerController not found!");
            return;
        }
    }

    private void Update()
    {
        if (!playerController.isLocalPlayer) return;

        if (Input.GetButtonDown("Jump"))
        {
            if (playerController.characterController.isGrounded)
            {
                currentJumpCount = 0; // Reset jumps on landing
            }

            if (currentJumpCount < maxJumps)
            {
                Jump();
            }
        }
    }

    private void Jump()
    {
        float jumpForce = PlayerEquipmentTracker.Instance.Jump;
        float gravity = playerController.gravity; // ✅ Uses CustomPlayerController gravity

        playerController.velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        currentJumpCount++;
    }

    // ✅ This function properly updates max jumps based on effects
    public void IncreaseMaxJumps(int amount)
    {
        maxJumps += amount;
        if (maxJumps < 1) maxJumps = 1; // Ensure at least one jump
        Debug.Log($"🔼 DoubleJumpHandler: Max Jumps updated to {maxJumps}");
    }
}
