using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class ObstacleAvoidancePathfinder : NetworkBehaviour
{
    [Header("Settings")]
    public float checkDistance = 20f;
    public float avoidanceDistance = 2f;
    public LayerMask obstacleMask;
    public bool isFlying = false;
    public bool drawDebug = true;
    private Vector3? activeAssistTarget = null;

    [Header("Trail Memory")]
    public float memoryInterval = 0.2f;
    public int maxTrailLength = 500;
    public float memoryTrackDistance = 30f;
    [Header("Collision Handling")]
    public float avoidanceRadius = 0.5f;
    private FlyHelperNode currentHelperNode = null;
    public float helperNodeSearchRadius = 15f;

    [HideInInspector]
    public Transform target;

    private PhysicsScene physicsScene;
    private List<Vector3> playerTrail = new List<Vector3>();
    private float lastTrailTime = -999f;
    private int trailWriteIndex = 0;
    private bool _playerVisible = true;
    public bool playerVisible => _playerVisible;

    private void Start()
    {
        physicsScene = gameObject.scene.GetPhysicsScene();
    }

    private void Update()
    {
        if (!isServer || target == null) return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (!Physics.Linecast(transform.position, target.position, obstacleMask))
        {
            // We can see the player
            _playerVisible = true;


            if (dist <= memoryTrackDistance && Time.time - lastTrailTime >= memoryInterval)
            {
                Vector3 pos = target.position;
                if (playerTrail.Count < maxTrailLength)
                {
                    playerTrail.Add(pos);
                }
                else
                {
                    playerTrail[trailWriteIndex] = pos;
                    trailWriteIndex = (trailWriteIndex + 1) % maxTrailLength;
                }

                lastTrailTime = Time.time;
            }
        }
        else
        {
            _playerVisible = false;

        }
    }
    public bool IsPathBlocked(Vector3 direction, float radius = 0.25f, float distance = 1.5f)
    {
        if (!isServer || !physicsScene.IsValid() || direction == Vector3.zero)
            return false;

        Vector3 start = transform.position + Vector3.up * 1f;
        Vector3 end = start + direction.normalized * distance;
        Vector3 rayDirection = (end - start).normalized;

        RaycastHit hit;
        bool hitSomething = physicsScene.SphereCast(start, radius, rayDirection, out hit, distance, obstacleMask);

        if (!hitSomething)
        {
            hitSomething = Physics.SphereCast(start, radius, rayDirection, out hit, distance, obstacleMask);
        }

        if (hitSomething)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.gameObject.layer == LayerMask.NameToLayer("IgnoreHits"))
            {
                if (drawDebug)
                    Debug.Log($"[Pathfinder] Ignored self or IgnoreHits: {hit.collider.name}");
                return false;
            }

            if (drawDebug)
            {
                Debug.DrawRay(start, rayDirection * hit.distance, Color.red, 1f);
                Debug.Log($"[Pathfinder] Blocked by: {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            return true;
        }

        if (drawDebug)
        {
            Debug.DrawRay(start, rayDirection * distance, Color.green, 1f);
        }

        return false;
    }

    public Vector3 GetAdjustedDirection()
    {
        if (!isServer || target == null) return Vector3.zero;

        Vector3 toPlayer = target.position - transform.position;
        Vector3 dirToPlayer = toPlayer.normalized;
        float distanceToPlayer = toPlayer.magnitude;

        if (physicsScene.Raycast(transform.position, dirToPlayer, out RaycastHit losHit, distanceToPlayer, obstacleMask))
        {
            if (losHit.collider.transform.IsChildOf(target))
            {
                if (drawDebug)
                {
                    Debug.DrawLine(transform.position, target.position, Color.green, 0.3f);
                    Debug.Log("[Pathfinder] Direct line to player — skipping avoidance.");
                }
                return dirToPlayer;
            }
            else
            {
                if (drawDebug)
                {
                    Debug.DrawRay(transform.position, dirToPlayer * losHit.distance, Color.red, 0.3f);
                    Debug.Log($"[Pathfinder] Line of sight blocked by: {losHit.collider.name}");
                }
            }
        }
        else
        {
            if (drawDebug)
            {
                Debug.DrawLine(transform.position, target.position, Color.green, 0.3f);
                Debug.Log("[Pathfinder] No obstacle hit — direct to player.");
            }
            return dirToPlayer;
        }

        Vector3 targetPosition = playerVisible && target != null
            ? target.position
            : GetBestTrailPoint();

        Vector3 toTarget = targetPosition - transform.position;
        Vector3 direction = toTarget.normalized;

        if (!physicsScene.SphereCast(transform.position, avoidanceRadius, direction, out RaycastHit hit, checkDistance, obstacleMask)
            || (target != null && hit.collider.transform.IsChildOf(target)))
        {
            return direction;
        }

        Vector3 altA = isFlying ? Quaternion.Euler(15f, 45f, 0) * direction : Quaternion.AngleAxis(45, Vector3.up) * direction;
        Vector3 altB = isFlying ? Quaternion.Euler(-15f, -45f, 0) * direction : Quaternion.AngleAxis(-45, Vector3.up) * direction;

        if (!physicsScene.SphereCast(transform.position, avoidanceRadius, altA, out _, avoidanceDistance, obstacleMask)) return altA;
        if (!physicsScene.SphereCast(transform.position, avoidanceRadius, altB, out _, avoidanceDistance, obstacleMask)) return altB;

        if (isFlying && target != null)
        {
            if (activeAssistTarget.HasValue)
            {
                Vector3 toAssist = activeAssistTarget.Value - transform.position;
                if (toAssist.magnitude < 1f)
                {
                    // Reached assist target
                    activeAssistTarget = null;
                    currentHelperNode = null;

                    if (drawDebug) Debug.Log("[Pathfinder] Reached assist target, resuming normal pathing.");
                }
                else
                {
                    if (drawDebug)
                    {
                        Debug.DrawLine(transform.position, activeAssistTarget.Value, Color.blue, 0.3f);
                        Debug.Log("[Pathfinder] Moving to active assist target.");
                    }
                    return toAssist.normalized;
                }
            }

            // Try helper nodes only if not already following one
            FlyHelperNode[] allNodes = GameObject.FindObjectsOfType<FlyHelperNode>();
            FlyHelperNode bestNode = null;
            float bestDistance = helperNodeSearchRadius;

            foreach (var node in allNodes)
            {
                float dist = Vector3.Distance(transform.position, node.transform.position);
                if (dist <= bestDistance)
                {
                    Vector3 dirToNode = (node.transform.position - transform.position).normalized;
                    float distToNode = Vector3.Distance(transform.position, node.transform.position);

                    if (!physicsScene.SphereCast(transform.position, avoidanceRadius, dirToNode, out _, distToNode, obstacleMask))
                    {
                        bestNode = node;
                        bestDistance = dist;
                    }
                }
            }

            if (bestNode != null)
            {
                currentHelperNode = bestNode;
                activeAssistTarget = bestNode.GetAssistTargetPosition();

                Vector3 toAssist = activeAssistTarget.Value - transform.position;

                if (drawDebug)
                {
                    Debug.DrawLine(transform.position, activeAssistTarget.Value, Color.cyan, 0.3f);
                    Debug.Log($"[Pathfinder] Starting assist path via {bestNode.name}");
                }

                return toAssist.normalized;
            }

            // No node, fallback pathing
            return Vector3.zero;
        }

        return Vector3.zero;
    }

    bool ClearPath(Vector3 from, Vector3 dir, float dist)
    {
        return !physicsScene.SphereCast(from, avoidanceRadius, dir, out _, dist, obstacleMask);
    }

    private Vector3 GetBestTrailPoint()
    {
        if (playerTrail.Count == 0) return transform.position;

        Vector3 best = playerTrail[trailWriteIndex];
        float bestDist = Vector3.Distance(transform.position, best);

        for (int i = 0; i < playerTrail.Count; i++)
        {
            float d = Vector3.Distance(transform.position, playerTrail[i]);
            if (d < bestDist)
            {
                best = playerTrail[i];
                bestDist = d;
            }
        }

        return best;
    }

    private void OnDrawGizmos()
    {
        if (!drawDebug || !Application.isPlaying || target == null) return;

        Vector3 adjusted = GetAdjustedDirectionEditorSafe();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + adjusted * 3f);

        Gizmos.color = Color.magenta;
        foreach (var point in playerTrail)
        {
            Gizmos.DrawSphere(point, 0.2f);
        }
    }
    private Vector3 GetAdjustedDirectionEditorSafe()
    {
        if (target == null) return Vector3.forward;

        Vector3 toTarget = target.position - transform.position;
        return toTarget.normalized;
    }

}
