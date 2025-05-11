using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    public float pickupDistance = 3f;
    public LayerMask pickupLayer;

    private PickupItem currentTarget;
    private PhysicsScene physicsScene;

    private void Start()
    {
        // Ensure we use the correct physics scene for this player’s scene
        physicsScene = gameObject.scene.GetPhysicsScene();

        Debug.Log($"[Pickup] Using PhysicsScene: {physicsScene.IsValid()} for {gameObject.scene.name}");
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleLookForPickup();
        HandlePickupInput();
    }

    private void HandleLookForPickup()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * pickupDistance, Color.yellow);

        if (physicsScene.Raycast(ray.origin, ray.direction, out hit, pickupDistance, pickupLayer))
        {
            PickupItem pickup = hit.collider.GetComponentInParent<PickupItem>();
            if (pickup != null)
            {
                if (pickup != currentTarget)
                {
                    ClearHighlight();
                    currentTarget = pickup;
                    currentTarget.SetHighlighted(true);
                    ShowTooltip(currentTarget);
                }
                return;
            }
        }

        ClearHighlight(); // Nothing hit or not a pickup
    }

    private void HandlePickupInput()
    {
        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            TryPickup(currentTarget);
        }
    }

    private void TryPickup(PickupItem pickup)
    {
        if (AddItemToInventory(pickup.itemId, pickup.quantity))
        {
            CharacterData.Current.Save();
            pickup.CmdDestroyPickup();
            ClearHighlight();
        }
    }

    private void ClearHighlight()
    {
        if (currentTarget != null)
        {
            currentTarget.SetHighlighted(false);
            HideTooltip();
            currentTarget = null;
        }
    }

    private void ShowTooltip(PickupItem pickup)
    {
        var item = pickup.GetItemData();
        if (item != null && WorldItemTooltipUI.Instance != null)
        {
            WorldItemTooltipUI.Instance.Show(item, pickup.transform);
        }
    }

    private void HideTooltip()
    {
        WorldItemTooltipUI.Instance?.Hide();
    }

    private bool AddItemToInventory(int itemId, int quantity)
    {
        if (CharacterData.Current == null || CharacterData.Current.Inventory == null)
        {
            Debug.LogError("❌ No active character or inventory found!");
            return false;
        }

        ItemSO itemData = ItemDatabaseSO.Instance.GetItemById(itemId.ToString());
        if (itemData == null)
        {
            Debug.LogError($"❌ Item ID {itemId} not found in database!");
            return false;
        }

        int remainingQuantity = quantity;

        // Try stacking first
        foreach (InventorySlot slot in CharacterData.Current.Inventory)
        {
            if (slot.ItemID == itemId.ToString() && slot.Quantity < itemData.maxStackSize)
            {
                int availableSpace = itemData.maxStackSize - slot.Quantity;
                int amountToAdd = Mathf.Min(availableSpace, remainingQuantity);

                slot.Quantity += amountToAdd;
                remainingQuantity -= amountToAdd;

                if (remainingQuantity <= 0) return true;
            }
        }

        // Place in empty slots
        foreach (InventorySlot slot in CharacterData.Current.Inventory)
        {
            if (slot.IsEmpty())
            {
                int amountToAdd = Mathf.Min(remainingQuantity, itemData.maxStackSize);
                slot.SetItem(itemId.ToString(), amountToAdd);
                remainingQuantity -= amountToAdd;

                if (remainingQuantity <= 0) return true;
            }
        }

        if (remainingQuantity > 0)
        {
            Debug.LogWarning($"❌ Not enough space for {remainingQuantity}x {itemData.itemName}! Inventory is full.");
            return false;
        }

        return true;
    }
}
