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

    private float lastDamageTime = -999f;

    private Rigidbody rb;
    private Transform player;
    private bool isGrounded;
    private bool isActive = false;
    private PhysicsScene physicsScene;

    private void Awake()
    {
        physicsScene = gameObject.scene.GetPhysicsScene();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // ✅ Only allow Y rotation
        StartCoroutine(CheckForPlayers());
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
                        FacePlayer();
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
            return true;
        }

        return false;
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
        if (!player || !isGrounded) return;

        FacePlayer(); // ✅ Make sure it's always facing the player before jumping

        Vector3 jumpDirection = (player.position - transform.position).normalized;
        jumpDirection.y = 1f;

        rb.velocity = Vector3.zero;
        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
    }

    [Server]
    private void JumpRandomly()
    {
        if (!isGrounded) return;

        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized;
        rb.velocity = Vector3.zero;
        rb.AddForce(randomDirection * jumpForce, ForceMode.Impulse);
    }

    private void FacePlayer()
    {
        if (!player) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0; // ✅ Keep the rotation only on the Y-axis

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
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
