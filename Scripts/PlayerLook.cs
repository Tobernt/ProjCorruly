using UnityEngine;
using UnityEngine.Animations;
using Mirror;

public class PlayerLook : NetworkBehaviour
{
    [Header("References")]
    public Animator animator;
    public RotationConstraint spineConstraint; // Adjusts torso bending
    public RotationConstraint headConstraint;  // Adjusts head tilt

    private float verticalRotation = 0f;

    private void Start()
    {
        if (!isLocalPlayer) return;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("🚨 No Animator found!");
            }
        }

        if (spineConstraint == null || headConstraint == null)
        {
            Debug.LogError("🚨 Rotation Constraints are missing! Assign them in Inspector.");
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || Camera.main == null) return;

        verticalRotation = Camera.main.transform.localEulerAngles.x;

        // Normalize vertical rotation (to avoid 360-degree flip issues)
        if (verticalRotation > 180f) verticalRotation -= 360f;

        ApplyRotationConstraints(verticalRotation);
    }

    private void ApplyRotationConstraints(float verticalRotation)
    {
        // Apply rotation to the SPINE (upper body bends up/down)
        if (spineConstraint != null)
        {
            Vector3 newOffset = spineConstraint.rotationOffset;
            newOffset.x = verticalRotation * 0.5f; // Adjust bending amount
            spineConstraint.rotationOffset = newOffset;
        }

        // Apply rotation to the HEAD (subtle tilt)
        if (headConstraint != null)
        {
            Vector3 newOffset = headConstraint.rotationOffset;
            newOffset.x = verticalRotation * 0.8f; // More responsive than spine
            headConstraint.rotationOffset = newOffset;
        }
    }
}
