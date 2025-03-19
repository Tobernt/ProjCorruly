using UnityEngine;
using Mirror;

public class NetworkedSelfDestruct : NetworkBehaviour
{
    public float lifetime = 2f;

    public override void OnStartServer()
    {
        Invoke(nameof(DestroySelf), lifetime);
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}
