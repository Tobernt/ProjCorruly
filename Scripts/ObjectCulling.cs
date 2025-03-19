using UnityEngine;

public class ObjectCulling : MonoBehaviour
{
    public Transform player;
    public float maxRenderDistance = 300f; // Distance at which objects disable

    private void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);
        gameObject.SetActive(distance < maxRenderDistance);
    }
}
