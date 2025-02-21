using UnityEngine;
using UnityEngine.Animations;
using Mirror;
using CustomNamespace; // ✅ Ensure correct namespace is included

public class PlayerLook : NetworkBehaviour
{
    [SerializeField] private Animator animator;  // Assign Animator in the Inspector
    [SerializeField] private RotationConstraint leftShoulderConstraint;
    [SerializeField] private RotationConstraint rightShoulderConstraint;
    [SerializeField] private RotationConstraint headConstraint;
    [SerializeField] private CustomNamespace.PlayerCamera playerCamera;

    [SerializeField] private float maxHeadAngle = 60f; // Prevents unnatural rotation

    private void Start()
    {
        if (!isLocalPlayer) return;

        if (playerCamera == null)
        {
            playerCamera = GetComponent<CustomNamespace.PlayerCamera>(); // ✅ Correctly reference PlayerCamera
            if (playerCamera == null)
            {
                Debug.LogError("PlayerLook: PlayerCamera component is missing!");
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("PlayerLook: Animator component is missing!");
            }
        }

        if (rightShoulderConstraint == null || headConstraint == null)
        {
            Debug.LogError("PlayerLook: Rotation Constraints are missing! Assign them in the Inspector.");
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || playerCamera == null || animator == null) return;

        float verticalRotation = Mathf.Clamp(playerCamera.GetVerticalRotation(), -maxHeadAngle, maxHeadAngle);

        ApplyRotationConstraints(verticalRotation);
    }

    private void ApplyRotationConstraints(float verticalRotation)
    {
        if (leftShoulderConstraint != null)
        {
            Vector3 newOffset = leftShoulderConstraint.rotationOffset;
            newOffset.x = verticalRotation;
            leftShoulderConstraint.rotationOffset = newOffset;
        }
        if (rightShoulderConstraint != null)
        {
            Vector3 newOffset = rightShoulderConstraint.rotationOffset;
            newOffset.x = verticalRotation;
            rightShoulderConstraint.rotationOffset = newOffset;
        }

        if (headConstraint != null)
        {
            Vector3 newOffset = headConstraint.rotationOffset;
            newOffset.x = verticalRotation;
            headConstraint.rotationOffset = newOffset;
        }
    }
}
