using UnityEngine;
using System.Collections.Generic;
using CustomNamespace;
using Mirror;
using System.Collections;

public class Hotbar : MonoBehaviour
{

    [Tooltip("References to inventory indices (0–Inventory.Count)")]
    public List<int> hotbarIndices = new List<int> { -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    public int selectedIndex = -1;
    public HotbarUI hotbarUI;
    private void Start()
    {
        StartCoroutine(EnsureWeaponSync());
    }

    private IEnumerator EnsureWeaponSync()
    {
        // Wait for network identity
        while (NetworkClient.connection?.identity == null)
            yield return null;

        if (!NetworkClient.connection.identity.TryGetComponent(out WeaponController weaponController))
            yield break;

        yield return null; // Wait a frame for hotbar setup

        // If nothing selected and weaponController has something equipped
        if (selectedIndex < 0 && !string.IsNullOrEmpty(weaponController.WeaponControllerID))
        {
            Debug.Log("🛑 No hotbar slot selected but weapon is still equipped. Forcing unequip.");
            weaponController.WeaponControllerID = "";
            weaponController.UpdateWeapon();
        }
    }

    public void LoadFromSerialized(string data)
    {
        var parts = data.Split(',');
        hotbarIndices.Clear();
        foreach (var part in parts)
        {
            if (int.TryParse(part, out int index))
                hotbarIndices.Add(index);
            else
                hotbarIndices.Add(-1);
        }
        Refresh();
    }

    public string SerializeToString()
    {
        return string.Join(",", hotbarIndices);
    }

    private void Refresh()
    {
        hotbarUI?.RefreshUI();
    }

    private void Update()
    {
        // Scroll through hotbar using number keys
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                return;
            }
        }

        // 🔁 Scroll hotbar with mouse wheel
        if (Input.mouseScrollDelta.y > 0f)
        {
            CycleSlot(-1); // Scroll up → previous
        }
        else if (Input.mouseScrollDelta.y < 0f)
        {
            CycleSlot(1);  // Scroll down → next
        }
    }
    private void CycleSlot(int direction)
    {
        if (hotbarIndices.Count == 0) return;

        int nextIndex = selectedIndex;

        do
        {
            nextIndex = (nextIndex + direction + hotbarIndices.Count) % hotbarIndices.Count;
        } while (hotbarIndices[nextIndex] < 0 && nextIndex != selectedIndex); // Skip empty slots

        SelectSlot(nextIndex);
    }

    public InventorySlot GetSlot(int hotbarSlotIndex)
    {
        if (hotbarSlotIndex < 0 || hotbarSlotIndex >= hotbarIndices.Count) return null;

        int inventoryIndex = hotbarIndices[hotbarSlotIndex];
        if (inventoryIndex < 0 || inventoryIndex >= CharacterData.Current.Inventory.Count) return null;

        return CharacterData.Current.Inventory[inventoryIndex];
    }

    public void AssignSlot(int hotbarSlotIndex, int inventorySlotIndex)
    {
        if (hotbarSlotIndex < 0 || hotbarSlotIndex >= hotbarIndices.Count) return;
        hotbarIndices[hotbarSlotIndex] = inventorySlotIndex;

        SyncHotbar();
        hotbarUI?.RefreshUI();
    }

    public void ClearSlot(int hotbarSlotIndex)
    {
        if (hotbarSlotIndex < 0 || hotbarSlotIndex >= hotbarIndices.Count) return;
        hotbarIndices[hotbarSlotIndex] = -1;
        if (selectedIndex == hotbarSlotIndex) selectedIndex = -1;

        if (NetworkClient.connection.identity.TryGetComponent(out WeaponController weaponController))
        {
            weaponController.UpdateWeapon(); // ⬅️ Add this to clear server/client weapon state
        }

        SyncHotbar();
        hotbarUI?.RefreshUI();
    }


    private void SyncHotbar()
    {
        if (NetworkClient.connection.identity.TryGetComponent(out CustomPlayerController player))
        {
            player.CmdUpdateHotbar(SerializeToString());
        }
    }

    public void SelectSlot(int hotbarSlotIndex)
    {
        selectedIndex = hotbarSlotIndex;
        UseSelected();

        if (NetworkClient.connection.identity.TryGetComponent(out WeaponController weaponController))
        {
            weaponController.UpdateWeapon();
        }

        hotbarUI?.RefreshUI();
    }

    public void UseSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= hotbarIndices.Count)
            return;

        int inventoryIndex = hotbarIndices[selectedIndex];
        if (inventoryIndex < 0 || inventoryIndex >= CharacterData.Current.Inventory.Count)
        {
            UnequipWeaponOnly();
            return;
        }

        InventorySlot slot = CharacterData.Current.Inventory[inventoryIndex];
        if (slot == null || slot.IsEmpty())
        {
            UnequipWeaponOnly();
            return;
        }

        string itemID = slot.ItemID;
        var item = ItemDatabaseSO.Instance.GetItemById(itemID);
        if (item == null)
        {
            UnequipWeaponOnly();
            return;
        }

        if (!NetworkClient.connection.identity.TryGetComponent(out CustomPlayerController player))
        {
            Debug.LogError("❌ Could not find local player to equip item.");
            return;
        }

        // Don't use rings from hotbar
        if (item.itemType == ItemSO.ItemType.Ring)
        {
            Debug.Log("⛔ Rings cannot be used from hotbar.");
            return;
        }

        // Consumables: use and update locally
        if (item.itemType == ItemSO.ItemType.Consumable)
        {
            Debug.Log($"🧪 Using consumable: {item.itemName}");
            slot.Quantity--;

            if (slot.Quantity <= 0)
            {
                slot.ClearItem();
                ClearSlot(selectedIndex);
            }

            CharacterData.Current.Save();
            InventoryUI.Instance?.RefreshUI();
            hotbarUI?.RefreshUI();
            return;
        }

        // 🔁 All gear (Weapon/Shield) equip must go through a Command
        player.CmdEquipFromHotbar(item.itemType.ToString(), itemID);
        player.EnableCombatMode();
    }

    private void UnequipWeaponOnly()
    {
        if (!NetworkClient.connection.identity.TryGetComponent(out CustomPlayerController player))
        {
            Debug.LogError("❌ Could not find local player to unequip.");
            return;
        }

        player.equipmentTracker.UnequipItem("Weapon");

        // ⬇️ Force local WeaponController to clear as well
        if (NetworkClient.connection.identity.TryGetComponent(out WeaponController weapon))
        {
            weapon.WeaponControllerID = ""; // clear
            weapon.UpdateWeapon();          // refresh visuals & disable ammoText
        }

        Debug.Log("🧼 Empty hotbar slot — unequipped weapon only.");
        CharacterData.Current.Save();
        InventoryUI.Instance?.RefreshUI();
        EquipmentUI.Instance?.RefreshUI();
        hotbarUI?.RefreshUI();
    }


    private void AddItemToInventory(string itemID)
    {
        InventorySlot emptySlot = CharacterData.Current.Inventory.Find(s => s.IsEmpty());
        if (emptySlot != null)
        {
            emptySlot.ItemID = itemID;
            emptySlot.Quantity = 1;
        }
        else
        {
            Debug.LogWarning("⚠️ No empty inventory slot — expanding inventory.");
            CharacterData.Current.Inventory.Add(new InventorySlot
            {
                ItemID = itemID,
                Quantity = 1
            });
        }
    }
}
