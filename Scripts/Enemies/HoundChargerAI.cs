using UnityEngine;
using Mirror;
using System.Collections;
using System.Linq;
using CustomNamespace;

public enum HoundState { Idle, Walking, Aggressive, Charging, Lunging }

public class HoundChargerAI : NetworkBehaviour
{
    [Header("AI Settings")]
    public HoundState currentState = HoundState.Idle;
    public float detectionRange = 15f;
    public float aggressionRange = 10f;
    public float lungeRange = 3f;
    public float chargeSpeed = 10f;
    public float turnSpeed = 3f;
    public float lungeForce = 12f;
    public float idleWalkSpeed = 2f;
    public float chargeCooldown = 3f;
    public float damageCooldown = 1f;
    public int damageAmount = 15;
    public float knockbackForce = 6f;
    public float idleTwitchInterval = 4f;
    public float obstacleAvoidanceRadius = 1f;
    private Vector3 startPosition;
    private Vector3? wanderTarget = null;
    private float wanderCooldown = 3f;
    private float lastWanderTime = -10f;
    private ObstacleAvoidancePathfinder pathfinder;
    private float lastChargeTime = -999f;
    private float lastDamageTime = -999f;
    private float idleTwitchTimer = 0f;

    private Transform player;
    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded = true;
    private string currentAnim = "";

    private float aggroAnimLength = 1.625f;
    private float aggroStartTime = -1f;
    private bool hasPlayedAggro = false;
    private float lungeAnimLength = 1.2f;
    private float lungeStartTime = -1f;
    private bool hasLunged = false;

    private void Start()
    {
        pathfinder = GetComponent<ObstacleAvoidancePathfinder>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;

        AnimationClip aggroClip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "Aggro");
        if (aggroClip != null)
            aggroAnimLength = aggroClip.length;

        StartCoroutine(CheckForPlayers());
    }
    private void Wander()
    {
        if (Time.time - lastWanderTime > wanderCooldown || wanderTarget == null)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            wanderTarget = startPosition + randomOffset;

            lastWanderTime = Time.time;
        }

        if (wanderTarget.HasValue)
        {
            Vector3 target = wanderTarget.Value;
            Vector3 dir = (target - transform.position).normalized;
            dir.y = 0;

            if (Vector3.Distance(transform.position, target) < 0.5f)
            {
                wanderTarget = null;
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            rb.MovePosition(rb.position + transform.forward * idleWalkSpeed * Time.deltaTime);
        }

        PlayAnim("IdleWalk");
    }
    private void ReturnToStart()
    {
        Vector3 dir = (startPosition - transform.position).normalized;
        dir.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        rb.MovePosition(rb.position + transform.forward * idleWalkSpeed * Time.deltaTime);

        PlayAnim("IdleWalk");

        if (Vector3.Distance(transform.position, startPosition) < 1f)
        {
            ChangeState(HoundState.Idle);
            wanderTarget = null;
        }
    }

    private void Update()
    {
        if (!isServer) return;
        if (player == null || player.CompareTag("Dead")) return;

        if (pathfinder != null)
            pathfinder.target = player;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case HoundState.Idle:
                idleTwitchTimer += Time.deltaTime;

                if (idleTwitchTimer >= idleTwitchInterval)
                {
                    if (Random.value < 0.3f) // 30% chance to twitch when interval hits
                    {
                        PlayAnim("IdleTwitch");
                    }
                    else
                    {
                        PlayAnim("Idle");
                    }

                    // Randomize next twitch interval a bit
                    idleTwitchInterval = Random.Range(3f, 6f);
                    idleTwitchTimer = 0f;
                }
                else
                {
                    PlayAnim("Idle");
                }

                if (dist <= detectionRange) ChangeState(HoundState.Walking);
                break;

            case HoundState.Walking:
                Wander();

                if (dist <= aggressionRange)
                    ChangeState(HoundState.Aggressive);
                else if (Vector3.Distance(transform.position, startPosition) > detectionRange * 2f)
                    ReturnToStart();

                PlayAnim("IdleWalk");
                if (dist <= aggressionRange) ChangeState(HoundState.Aggressive);
                break;

            case HoundState.Aggressive:
                if (!hasPlayedAggro)
                {
                    PlayAnim("Aggro");
                    aggroStartTime = Time.time;
                    hasPlayedAggro = true;
                }

                // Continuously rotate while aggro animation plays
                FaceTarget();

                if (Time.time - aggroStartTime >= aggroAnimLength)
                {
                    ChangeState(HoundState.Charging);
                    hasPlayedAggro = false;
                }

                if (Time.time - aggroStartTime >= aggroAnimLength)
                {
                    ChangeState(HoundState.Charging);
                    hasPlayedAggro = false;
                }
                break;

            case HoundState.Charging:
                PlayAnim("Charge");
                ChasePlayer();
                if (dist <= lungeRange && Time.time - lastChargeTime > chargeCooldown)
                {
                    ChangeState(HoundState.Lunging);
                }
                if (dist > detectionRange * 1.5f) // Grace buffer
                {
                    ChangeState(HoundState.Walking);
                    hasPlayedAggro = false;
                    return;
                }

                break;

            case HoundState.Lunging:
                if (!hasLunged)
                {
                    FaceTarget();
                    PlayAnim("Attack");
                    LungeAtPlayer();
                    lungeStartTime = Time.time;
                    hasLunged = true;
                }
                if (Time.time - lungeStartTime >= lungeAnimLength)
                {
                    ChangeState(HoundState.Charging);
                    hasLunged = false;
                }
                break;
        }
    }

    private void PlayAnim(string name, float transition = 0.15f)
    {
        if (animator == null || currentAnim == name) return;
        animator.CrossFade(name, transition);
        currentAnim = name;
    }

    private void ChangeState(HoundState newState)
    {
        currentState = newState;
    }

    private IEnumerator CheckForPlayers()
    {
        while (!FindPlayer())
        {
            yield return new WaitForSeconds(1f);
        }
    }

    private bool FindPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = detectionRange;
        Transform closest = null;

        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < closestDist)
            {
                closest = p.transform;
                closestDist = dist;
            }
        }

        if (closest != null)
        {
            player = closest;
            return true;
        }

        return false;
    }

    private void WalkAround()
    {
        transform.Translate(Vector3.forward * idleWalkSpeed * Time.deltaTime);
    }

    private void ChasePlayer()
    {
        Vector3 direction = pathfinder != null && player != null
            ? pathfinder.GetAdjustedDirection()
            : (player.position - transform.position).normalized;
        direction.y = 0;

        // Obstacle check
        if (Physics.SphereCast(transform.position, obstacleAvoidanceRadius, transform.forward, out RaycastHit hit, 1f))
        {
            Debug.Log($"Blocked by {hit.collider.name} (scene-based cast)");
            if (!hit.collider.CompareTag("Player")) return; // Stop if blocked by something that's not the player
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        rb.MovePosition(rb.position + transform.forward * chargeSpeed * Time.deltaTime);
    }

    private void FaceTarget()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    private void LungeAtPlayer()
    {
        if (!isGrounded) return;
        Vector3 lungeDir = (player.position - transform.position).normalized + Vector3.up * 0.25f;
        rb.AddForce(lungeDir * lungeForce, ForceMode.Impulse);
        lastChargeTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer || Time.time - lastDamageTime < damageCooldown) return;

        GameObject hitObject = collision.gameObject;

        if (!hitObject.CompareTag("Player")) return;

        CustomPlayerController controller = hitObject.GetComponent<CustomPlayerController>();
        if (controller != null && controller.connectionToClient != null)
        {
            Vector3 knockbackDir = (hitObject.transform.position - transform.position).normalized;
            knockbackDir.y = 0f;
            controller.TargetApplyKnockback(controller.connectionToClient, knockbackDir, knockbackForce);
        }

        PlayerHealth health = hitObject.GetComponent<PlayerHealth>() ?? hitObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damageAmount, gameObject);
            lastDamageTime = Time.time;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}
