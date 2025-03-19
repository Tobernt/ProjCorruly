using Mirror;
using UnityEngine;

public class PickupItem : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnItemChanged))] public int itemId;
    [SyncVar] public int quantity;

    [SerializeField] private ItemDatabaseSO itemDatabase;
    private ItemSO itemData;
    private GameObject modelInstance;

    public override void OnStartClient()
    {
        base.OnStartClient();
        LoadItemData(); // Ensure clients load item data properly
    }

    private void Start()
    {
        if (itemDatabase == null)
        {
            Debug.LogError("❌ ItemDatabaseSO is missing! Assign it in the Inspector.");
            return;
        }

        if (isServer)
        {
            LoadItemData();
        }
    }
    [Command(requiresAuthority = false)]
    public void CmdDestroyPickup()
    {
        if (!isServer) return; // ✅ Ensure only the server runs this
        RpcDestroyPickup();
        NetworkServer.Destroy(gameObject);
    }


    [ClientRpc]
    void RpcDestroyPickup()
    {
        if (gameObject != null) Destroy(gameObject);
    }


    private void LoadItemData()
    {
        itemData = itemDatabase.GetItemById(itemId.ToString());
        if (itemData == null)
        {
            Debug.LogError($"❌ No item found for ID: {itemId}");
            return;
        }

        Debug.Log($"✅ Loaded item: {itemData.name}");

            RpcSpawnItemModel(itemId); // Ensure all clients spawn the model
    }

    private void OnItemChanged(int oldId, int newId)
    {
        RpcSpawnItemModel(newId); // Ensure clients update their model
    }

    [ClientCallback]
    private void RpcSpawnItemModel(int itemId)
    {
        if (modelInstance != null)
        {
            Destroy(modelInstance);
        }

        itemData = itemDatabase.GetItemById(itemId.ToString());
        if (itemData == null || itemData.itemPrefab == null)
        {
            Debug.LogError($"❌ No prefab assigned for item {itemId}");
            return;
        }

        modelInstance = Instantiate(itemData.itemPrefab, transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;

        Debug.Log($"✅ Spawned world item model: {itemData.name}");
    }
}
