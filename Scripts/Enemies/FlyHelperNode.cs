using UnityEngine;

[ExecuteAlways]
public class FlyHelperNode : MonoBehaviour
{
    [Header("Assist Movement Direction")]
    public Vector3 assistDirection = Vector3.up;
    public float assistDistance = 3f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        // Show node position
        Gizmos.DrawSphere(transform.position, 0.2f);

        // Show direction vector
        Vector3 worldDir = transform.TransformDirection(assistDirection.normalized) * assistDistance;
        Gizmos.DrawLine(transform.position, transform.position + worldDir);

        // Arrowhead
        Vector3 right = Quaternion.LookRotation(worldDir) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(worldDir) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
        Gizmos.DrawLine(transform.position + worldDir, transform.position + worldDir + right * 0.5f);
        Gizmos.DrawLine(transform.position + worldDir, transform.position + worldDir + left * 0.5f);
    }

    public Vector3 GetAssistTargetPosition()
    {
        return transform.position + transform.TransformDirection(assistDirection.normalized) * assistDistance;
    }
}
