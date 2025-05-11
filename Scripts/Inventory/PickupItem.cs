using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupItem : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnItemChanged))] public int itemId;
    [SyncVar] public int quantity;

    [SerializeField] private ItemDatabaseSO itemDatabase;
    [SerializeField] private Material outlineMaterial;

    private ItemSO itemData;
    private GameObject modelInstance;
    private List<Material> outlineMaterials = new();
    private Coroutine outlineFadeCoroutine;

    public void Initialize(string itemIdStr, int qty)
    {
        itemData = itemDatabase.GetItemById(itemIdStr);
        if (itemData == null)
        {
            Debug.LogWarning($"❌ Could not find item with ID: {itemIdStr}");
            return;
        }

        itemId = itemData.itemId.GetHashCode();
        quantity = qty;

        if (isServer)
        {
            RpcSpawnItemModel(itemId);
        }
    }

    private void OnItemChanged(int oldId, int newId)
    {
        RpcSpawnItemModel(newId);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (itemId != 0 && modelInstance == null)
        {
            SpawnItemModelLocally(itemId);
        }
    }

    [ClientRpc]
    private void RpcSpawnItemModel(int itemId)
    {
        SpawnItemModelLocally(itemId);
    }

    private void SpawnItemModelLocally(int id)
    {
        if (modelInstance != null)
            Destroy(modelInstance);

        itemData = itemDatabase.GetItemById(id.ToString());
        if (itemData == null || itemData.itemPrefab == null)
        {
            Debug.LogError($"❌ No prefab for item {id}");
            return;
        }

        modelInstance = Instantiate(itemData.itemPrefab, transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;

        outlineMaterials.Clear();
        foreach (Renderer rend in modelInstance.GetComponentsInChildren<Renderer>())
        {
            var baseMats = rend.sharedMaterials;
            var newMats = new Material[baseMats.Length + 1];
            baseMats.CopyTo(newMats, 0);

            Material outline = new Material(outlineMaterial);
            outline.SetFloat("_OutlineWidth", 0f);
            newMats[newMats.Length - 1] = outline;

            rend.materials = newMats;
            outlineMaterials.Add(outline);
        }

        Debug.Log($"✅ Model + outline assigned for item: {itemData.name}");
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outlineFadeCoroutine != null)
            StopCoroutine(outlineFadeCoroutine);

        outlineFadeCoroutine = StartCoroutine(FadeOutline(highlighted ? 0.03f : 0f));
    }

    private IEnumerator FadeOutline(float targetWidth)
    {
        float duration = 0.1f;
        float t = 0f;
        float startWidth = outlineMaterials.Count > 0 ? outlineMaterials[0].GetFloat("_OutlineWidth") : 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float current = Mathf.Lerp(startWidth, targetWidth, t);

            foreach (var mat in outlineMaterials)
            {
                if (mat.HasProperty("_OutlineWidth"))
                    mat.SetFloat("_OutlineWidth", current);
            }

            yield return null;
        }

        foreach (var mat in outlineMaterials)
        {
            if (mat.HasProperty("_OutlineWidth"))
                mat.SetFloat("_OutlineWidth", targetWidth);
        }

        outlineFadeCoroutine = null;
    }

    public ItemSO GetItemData()
    {
        return itemData;
    }

    [Command(requiresAuthority = false)]
    public void CmdDestroyPickup()
    {
        if (!isServer) return;
        RpcDestroyPickup();
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcDestroyPickup()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }
}
