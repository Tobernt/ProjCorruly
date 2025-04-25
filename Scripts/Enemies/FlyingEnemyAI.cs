using UnityEngine;
using Mirror;
using System.Collections;
using CustomNamespace;

public enum SoulState { Idle, Chasing, RandomFly, Charging }

public class EaterOfSoulsAI : NetworkBehaviour
{
    [Header("AI Settings")]
    public SoulState currentState = SoulState.Idle;
    public float speed = 5f;
    public float detectionRange = 15f;
    public float deAggroRange = 20f;
    public float chargeRange = 5f; // Distance at which AI prepares to charge
    public float chargeSpeed = 10f; // Speed of the charge attack
    public float chargePauseTime = 0.5f; // Time before charging
    public float chargeTimeout = 3f; // Maximum time allowed for charging
    public float swoopSpeed = 2f;
    public float swoopArcHeight = 1.5f;
    public float wanderRadius = 5f;
    public Vector3 headOffset = new Vector3(0, 1.5f, 0); // Used for swooping attacks
    private Vector3 repositionDirection; // Final direction to move during repositioning
    private int curveDirection = 1;      // 1 = right, -1 = left
    public Vector3 bodyOffset = new Vector3(0, 1f, 0); // Used for charging attacks
    private float repositionCooldown = 1.5f;
    private float repositionEndTime = 3f;
    private Vector3 lastSwoopDirection;
    public float rotationSpeed = 5f;
    public float movementDelayVariance = 0.2f; // +/- 200ms delay variation
    [Header("Combat Settings")]
    public int attackDamage = 10;
    public float knockbackForce = 5f;
    [Header("Damage Cooldown")]
    public float damageCooldown = 0.5f;
    private Animator animator;
    private ObstacleAvoidancePathfinder pathfinder;
    private bool recoveringFromCharge = false;
    public float recoveryHeight = 5f;
    public float recoveryLiftSpeed = 4f;
    [Header("Recovery Settings")]
    public float recoveryLiftDuration = 1.5f; // Editor-set duration
    private float recoveryStartTime = -999f;

    private float regainHeightCooldown = 1.5f;
    private float lastHeightCheck = -999f;
    private bool regainHeightMode = false;


    private float lastDamageTime = -999f;

    private Transform player;
    private Rigidbody rb;
    private float swoopTimer = 0f;
    private Vector3 chargeTarget;
    private bool isCharging = false;
    private float chargeStartTime;
    private PhysicsScene physicsScene;

    private void Start()
    {
        physicsScene = gameObject.scene.GetPhysicsScene();
        pathfinder = GetComponent<ObstacleAvoidancePathfinder>();
        if (pathfinder != null && player != null)
            pathfinder.target = player;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        animator = GetComponent<Animator>();

        // Randomize initial animation cycle phase for variety
        if (animator != null)
        {
            float randomOffset = Random.Range(0f, 1f);
            animator.Play("MovingFlying", 0, randomOffset);
        }
    }

    private void Update()
    {
        if (!isServer) return;
        if (recoveringFromCharge)
        {
            RecoverHeight();
            return;
        }

        UpdateAnimation(); // Add this line to control animation
        if (CompareTag("Dead"))
        {
            speed = 0f;
            swoopSpeed = 0f;
            rb.velocity = Vector3.zero;
            return;
        }
        switch (currentState)
        {
            case SoulState.Idle:
                FindPlayer();
                if (player != null && !player.CompareTag("Dead"))
                {
                    ChangeState(SoulState.Chasing);
                }
                else
                {
                    Wander();
                }
                break;

            case SoulState.Chasing:
                if (player == null || player.CompareTag("Dead") || Vector3.Distance(transform.position, player.position) > deAggroRange)
                {
                    player = null;
                    ChangeState(SoulState.Idle);
                }
                else if (Vector3.Distance(transform.position, player.position) < chargeRange && !isCharging)
                {
                    StartCoroutine(PrepareCharge());
                }
                else
                {
                    SwoopTowardsPlayer();
                }
                break;

            case SoulState.Charging:
                if (player == null || player.CompareTag("Dead"))
                {
                    player = null;
                    ChangeState(SoulState.Idle);
                    return;
                }
                ChargeTowardsTarget();
                break;

            case SoulState.RandomFly:
                Wander();
                break;
        }

        RotateTowardsMovementDirection();
    }


    private void UpdateAnimation()
    {
        if (animator == null) return;

        // This sets the Animator's bool, which triggers transitions and blending
        animator.SetBool("IsCharging", currentState == SoulState.Charging);
    }

    private void FindPlayer()
    {
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
        }
    }

    [Server]
    private void SwoopTowardsPlayer()
    {
        if (!player || pathfinder == null) return;

        Vector3 target;

        if (Time.time < repositionEndTime)
        {
            // ✅ Curved reposition movement
            target = transform.position + repositionDirection * 5f;
        }
        else
        {
            // Regular swoop target (head offset included)
            Vector3 offset = Vector3.zero; // Can randomize later if needed
            target = player.position + headOffset + offset + Vector3.up * 4f; // Raise chase target slightly

        }

        // ✅ Use pathfinder for obstacle-aware flying direction
        Vector3 direction = pathfinder.GetAdjustedDirection();

        if (direction == Vector3.zero)
        {
            rb.velocity = Vector3.zero;
            return; // Don't move if fully blocked
        }

        // ✅ Apply arcing swoop motion
        swoopTimer += Time.deltaTime * swoopSpeed;
        float arcEffect = Mathf.Sin(swoopTimer) * swoopArcHeight;

        Vector3 moveDirection = new Vector3(direction.x, direction.y + arcEffect, direction.z).normalized;
        rb.velocity = moveDirection * speed * GetRandomMovementMultiplier();
    }


    [Server]
    private void Wander()
    {
        if (rb.velocity.magnitude < 1f)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-wanderRadius, wanderRadius),
                Random.Range(-wanderRadius / 2, wanderRadius / 2),
                Random.Range(-wanderRadius, wanderRadius)
            ).normalized;

            rb.velocity = randomDirection * speed * GetRandomMovementMultiplier();
        }
    }

    private void RotateTowardsMovementDirection()
    {
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private IEnumerator PrepareCharge()
    {
        isCharging = true;
        rb.velocity = Vector3.zero;

        yield return new WaitForSeconds(chargePauseTime); // Pause before charging

        // ✅ Determine charge target AFTER waiting (and use player's BODY, not head)
        if (player != null)
        {
            chargeTarget = player.position + bodyOffset;
        }

        chargeStartTime = Time.time; // Record the time charge started
        ChangeState(SoulState.Charging);
    }

    [Server]
    private void ChargeTowardsTarget()
    {
        Vector3 direction = (chargeTarget - transform.position).normalized;
        rb.velocity = direction * chargeSpeed;

        if (Vector3.Distance(transform.position, chargeTarget) < 1f || (Time.time - chargeStartTime) > chargeTimeout)
        {
            isCharging = false;

            curveDirection = Random.value < 0.5f ? -1 : 1;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, toPlayer).normalized * curveDirection;
            repositionDirection = (toPlayer * -1f + side).normalized;

            repositionEndTime = Time.time + repositionCooldown;

            recoveringFromCharge = true;
            if (animator != null)
            {
                animator.SetBool("IsCharging", false);
                animator.SetBool("IsRecovering", true);
            }

            recoveryStartTime = Time.time;
        }
    }
    private void RecoverHeight()
    {
        if (Time.time - recoveryStartTime >= recoveryLiftDuration)
        {
            recoveringFromCharge = false;

            if (animator != null)
                animator.SetBool("IsRecovering", false);

            ChangeState(SoulState.Chasing);
            return;
        }


        Vector3 upward = Vector3.up * recoveryLiftSpeed;
        rb.velocity = upward;
    }

    private IEnumerator ResumeChaseAfterLift()
    {
        yield return new WaitForSeconds(0.3f); // Let it lift briefly before chasing again
        ChangeState(SoulState.Chasing);
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


    private float GetRandomMovementMultiplier()
    {
        return Random.Range(1f - movementDelayVariance, 1f + movementDelayVariance);
    }

    private void ChangeState(SoulState newState)
    {
        if (!isServer) return;

        currentState = newState;

        // Immediately update Animator parameter to match
        if (animator != null)
        {
            animator.SetBool("IsCharging", newState == SoulState.Charging);
        }
    }
}
