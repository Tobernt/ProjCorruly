using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainBoltVisual : MonoBehaviour
{
    private Vector3 targetPosition;
    private float travelSpeed;
    private bool isMoving = false;

    private PhysicsScene physicsScene;

    public void SetTarget(Vector3 target, float speed)
    {
        targetPosition = target;
        travelSpeed = speed;
        isMoving = true;

        // Cache the physics scene the object is in
        physicsScene = gameObject.scene.GetPhysicsScene();

        Debug.Log($"[VisualBolt] From {transform.position} to {targetPosition} at speed {travelSpeed}");
    }

    private void Update()
    {
        if (!isMoving) return;

        // Use physics scene if valid
        if (physicsScene.IsValid())
        {
            Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, travelSpeed * Time.deltaTime);

            // If using rigidbody-based movement later, you’d do physicsScene.Simulate(...) here instead
            transform.position = nextPosition;

            if (Vector3.Distance(transform.position, targetPosition) <= 0.05f)
            {
                isMoving = false;
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogWarning("[VisualBolt] Invalid physics scene! Destroying.");
            Destroy(gameObject);
        }
    }

    // Optional: Ensure this object gets explicitly moved to the correct Unity scene from outside
    public void MoveToScene(Scene targetScene)
    {
        SceneManager.MoveGameObjectToScene(gameObject, targetScene);
        physicsScene = targetScene.GetPhysicsScene();
    }
}
