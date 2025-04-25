using Mirror;
using UnityEngine;

public class PierceHandler : MonoBehaviour
{
    public int remainingPierces = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Punchable"))
        {
            remainingPierces--;
            if (remainingPierces < 0)
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
