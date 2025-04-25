
using UnityEngine;

public class WallSpawnerOnHit : MonoBehaviour
{
    public GameObject wallPrefab;

    private void OnDestroy()
    {
        if (wallPrefab != null)
        {
            Instantiate(wallPrefab, transform.position, Quaternion.identity);
        }
    }
}
