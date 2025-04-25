using UnityEngine;
using Mirror;
using System.Collections;
using CustomNamespace;

public enum SlimeState { Idle, Jumping, Chasing, RandomJump }

public class SlimeAI : NetworkBehaviour
{
    [Header("AI Settings")]
    public SlimeState currentState = SlimeState.Idle;
    public float jumpForce = 5f;
    public float jumpInterval = 2f;
    public float detectionRange = 10f;
    public float deAggroRange = 15f;
    public float knockbackForce = 5f;
    public int attackDamage = 10;
    public float rotationSpeed = 5f; // Smooth turning speed
    [Header("Damage Cooldown")]
    public float damageCooldown = 0.5f;
    private ObstacleAvoidancePathfinder pathfinder;

    private float lastDamageTime = -999f;

    private bool isJumping = false;
    private Animator animator;

    private Rigidbody rb;
    private Transform player;
    private bool isGrounded;
    private bool isActive = false;
    private PhysicsScene physicsScene;

    private void Start()
    {
        physicsScene = gameObject.scene.GetPhysicsScene();
        pathfinder = GetComponent<ObstacleAvoidancePathfinder>();
        if (pathfinder != null && player != null)
            pathfinder.target = player;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // ✅ Only allow Y rotation
        StartCoroutine(CheckForPlayers());
    }
    private void Update()
    {
        if (!isServer) return;
        if (CompareTag("Dead"))
        {
            jumpForce = 0f;
            rotationSpeed = 0f;
            rb.velocity = Vector3.zero;
            return;
        }
        if (isGrounded && !isJumping &&
            currentState == SlimeState.Chasing &&
            player != null &&
            !player.CompareTag("Dead") &&
            Vector3.Distance(transform.position, player.position) <= deAggroRange)
        {
            FacePlayer();
        }
    }


    private IEnumerator CheckForPlayers()
    {
        while (!FindPlayer()) // ✅ Wait until a player is found
        {
            yield return new WaitForSeconds(1f);
        }

        isActive = true;

        // ✅ Random reaction delay (100ms - 300ms) before activating AI
        float reactionDelay = Random.Range(0.1f, 0.3f);
        yield return new WaitForSeconds(reactionDelay);

        StartCoroutine(FSM());
    }
    private IEnumerator FSM()
    {
        while (isActive)
        {
            switch (currentState)
            {
                case SlimeState.Idle:
                    float randomJumpDelay = Random.Range(jumpInterval * 0.8f, jumpInterval * 1.2f);
                    yield return new WaitForSeconds(randomJumpDelay);

                    if (FindPlayer() && !player.CompareTag("Dead"))
                        ChangeState(SlimeState.Chasing);
                    else
                        ChangeState(SlimeState.RandomJump);
                    break;

                case SlimeState.Chasing:
                    if (player == null || player.CompareTag("Dead") || Vector3.Distance(transform.position, player.position) > deAggroRange)
                    {
                        if (!FindPlayer() || (player != null && player.CompareTag("Dead")))
                        {
                            ChangeState(SlimeState.Idle);
                        }
                    }
                    else
                    {
                        Vector3 moveDir = pathfinder != null
                            ? pathfinder.GetAdjustedDirection()
                            : (player.position - transform.position).normalized;

                        // Always rotate toward the move direction
                        if (moveDir.sqrMagnitude > 0.01f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(new Vector3(moveDir.x, 0, moveDir.z));
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                        }

                        // Only jump if there's a valid path (handled in JumpTowardsPlayer)
                        JumpTowardsPlayer();
                        yield return new WaitForSeconds(jumpInterval);
                    }
                    break;


                case SlimeState.RandomJump:
                    JumpRandomly();
                    yield return new WaitForSeconds(jumpInterval);
                    ChangeState(SlimeState.Idle);
                    break;
            }
            yield return null;
        }
    }

    private bool FindPlayer()
    {
        if (!isServer) return false;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float closestDistance = detectionRange;

        foreach (GameObject p in players)
        {
            float distance = Vector3.Distance(transform.position, p.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = p.transform;
            }
        }

        if (closestPlayer != null)
        {
            player = closestPlayer;
            if (pathfinder != null)
                pathfinder.target = player;
            return true;
        }


        return false;
    }

    private bool IsFacingTarget(Transform target, float angleThreshold = 5f)
    {
        if (!target) return false;

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0;

        if (directionToTarget.sqrMagnitude < 0.01f) return true;

        float angle = Vector3.Angle(transform.forward, directionToTarget.normalized);
        return angle <= angleThreshold;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        GameObject hitObject = collision.gameObject;

        if (!hitObject.CompareTag("Player")) return;

        // ✅ Apply knockback using TargetRpc on CustomPlayerController
        CustomPlayerController controller = hitObject.GetComponent<CustomPlayerController>();
        if (controller != null && controller.connectionToClient != null)
        {
            Vector3 knockbackDir = (hitObject.transform.position - transform.position).normalized;
            knockbackDir.y = 0f;

            controller.TargetApplyKnockback(controller.connectionToClient, knockbackDir, knockbackForce);
        }

        // ✅ Apply damage
        PlayerHealth health = hitObject.GetComponent<PlayerHealth>() ?? hitObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(attackDamage, gameObject);
            lastDamageTime = Time.time;
        }
    }


    [Server]
    private void JumpTowardsPlayer()
    {
        if (!player || !isGrounded || pathfinder == null) return;

        Vector3 direction = pathfinder.GetAdjustedDirection();
        if (direction == Vector3.zero) return;

        // ✅ Use centralized logic from pathfinder
        if (pathfinder.IsPathBlocked(direction, 1f, 6f)) return;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f);

        direction.y = 1f;
        rb.velocity = Vector3.zero;
        rb.AddForce(direction.normalized * jumpForce, ForceMode.Impulse);
        isJumping = true;

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }


    [Server]
    private void JumpRandomly()
    {
        if (!isGrounded) return;

        // Generate direction first
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        // Face in that direction
        if (randomDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(randomDirection);
            transform.rotation = targetRotation;

            // Optional: add turning animation if you want
            if (animator != null)
            {
                animator.SetBool("IsTurningLeft", false);
                animator.SetBool("IsTurningRight", false);
            }
        }

        // Add jump force upward and in that direction
        Vector3 jumpDir = randomDirection + Vector3.up;
        rb.velocity = Vector3.zero;
        rb.AddForce(jumpDir.normalized * jumpForce, ForceMode.Impulse);

        isJumping = true;
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }



    private void FacePlayer()
    {
        if (!player || animator == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);

        Vector3 cross = Vector3.Cross(transform.forward, direction.normalized);
        float turnDir = Mathf.Sign(cross.y);

        if (angleDiff > 5f)
        {
            animator.SetBool("IsTurningLeft", turnDir < 0);
            animator.SetBool("IsTurningRight", turnDir > 0);
        }
        else
        {
            animator.SetBool("IsTurningLeft", false);
            animator.SetBool("IsTurningRight", false);
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }


    private void ChangeState(SlimeState newState)
    {
        if (!isServer) return;
        currentState = newState;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            // Reset jumping status once grounded again
            if (isJumping)
            {
                isJumping = false;
            }
        }
    }


    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}