using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class InventorySlotContextMenu : MonoBehaviour
{
    public static InventorySlotContextMenu Instance { get; private set; }

    public GameObject menuPanel;
    public Button useButton, equipButton, splitButton, dropButton, destroyButton;
    private InventorySlotUI selectedSlot;

    private void Awake()
    {
        Instance = this;
        menuPanel?.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            HideMenu();
        }
    }

    public void ShowMenu(InventorySlotUI slotUI)
    {
        if (menuPanel == null || CharacterData.Current?.Inventory == null) return;

        selectedSlot = slotUI;
        InventorySlot slot = CharacterData.Current.Inventory[slotUI.slotIndex];
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(slot.ItemID);

        if (slot.IsEmpty() || item == null)
        {
            menuPanel.SetActive(false);
            return;
        }

        useButton.gameObject.SetActive(item.itemType == ItemSO.ItemType.Consumable);
        equipButton.gameObject.SetActive(IsEquippable(item));
        splitButton.gameObject.SetActive(slot.Quantity > 1);

        menuPanel.transform.position = KeepWithinScreenBounds(Input.mousePosition);
        menuPanel.SetActive(true);

        AssignButtonActions();
    }

    private void AssignButtonActions()
    {
        useButton.SetListener(UseItem);
        equipButton.SetListener(EquipOrUnequipItem);
        splitButton.SetListener(SplitItem);
        dropButton.SetListener(DropItem);
        destroyButton.SetListener(DestroyItem);
    }

    private Vector3 KeepWithinScreenBounds(Vector3 pos)
    {
        RectTransform rect = menuPanel.GetComponent<RectTransform>();
        return new Vector3(
            Mathf.Clamp(pos.x, 0, Screen.width - rect.rect.width),
            Mathf.Clamp(pos.y, rect.rect.height, Screen.height),
            pos.z
        );
    }

    private bool IsEquippable(ItemSO item) => Array.Exists(new[]
    {
        ItemSO.ItemType.Weapon, ItemSO.ItemType.Shield, ItemSO.ItemType.Ring
    }, t => t == item.itemType);

    private bool IsItemEquipped(ItemSO item)
    {
        var inv = CharacterData.Current;

        if (item.itemType == ItemSO.ItemType.Ring)
        {
            return false; // Rings are allowed multiple times, no need to check
        }

        return inv.MainHand?.ItemID == item.itemId || inv.Offhand?.ItemID == item.itemId;
    }



    private void UseItem() => ExecuteAction($"🛠 Using item {selectedSlot.slotIndex}");
    private void EquipOrUnequipItem()
    {
        InventorySlot slot = CharacterData.Current.Inventory[selectedSlot.slotIndex];

        if (slot.IsEmpty())
        {
            Debug.LogError("❌ No item to equip or unequip.");
            return;
        }

        ItemSO item = ItemDatabaseSO.Instance.GetItemById(slot.ItemID);
        if (item == null)
        {
            Debug.LogError($"❌ Item data not found for ID {slot.ItemID}");
            return;
        }

            EquipItem(item, selectedSlot.slotIndex);
        HideMenu(); // ✅ Close menu after equipping/unequipping
    }
    private void EquipItem(ItemSO item, int inventoryIndex)
    {
        InventorySlot inventorySlot = CharacterData.Current.Inventory[inventoryIndex];
        EquipmentSlot equipmentSlot;

        if (item.itemType == ItemSO.ItemType.Ring)
        {
            int availableSlot = EquipmentUI.Instance.FindFirstAvailableRingSlot();
            if (availableSlot == -1)
            {
                Debug.LogWarning("⚠ No available ring slots left! Consider unequipping a ring first.");
                return;
            }

            int ringArrayIndex = availableSlot - 2; // Convert UI index (2-6) → Rings[0-4]
            equipmentSlot = CharacterData.Current.Rings[ringArrayIndex];

            if (equipmentSlot == null)
            {
                Debug.LogError($"❌ Equipment slot {availableSlot} returned null.");
                return;
            }

            Debug.Log($"✅ Equipping {item.itemName} to ring slot {availableSlot} (Rings[{ringArrayIndex}])");
        }
        else if (item.itemType == ItemSO.ItemType.Weapon || item.itemType == ItemSO.ItemType.Shield)
        {
            // Weapons go into MainHand (slot 0), Shields into Offhand (slot 1)
            equipmentSlot = GetEquipmentSlotByType(item.itemType);

            if (equipmentSlot == null)
            {
                Debug.LogError($"❌ No valid equipment slot found for {item.itemType}");
                return;
            }

            Debug.Log($"✅ Equipping {item.itemName} to {item.itemType} slot.");
        }
        else
        {
            Debug.LogError($"❌ {item.itemType} cannot be equipped.");
            return;
        }

        // ✅ Swap logic for all items
        if (equipmentSlot.IsOccupied)
        {
            string tempItemId = equipmentSlot.ItemID;
            equipmentSlot.EquipItem(inventorySlot.ItemID);
            inventorySlot.SetItem(tempItemId);
        }
        else
        {
            equipmentSlot.EquipItem(inventorySlot.ItemID);
            inventorySlot.ClearItem();
        }

        CharacterData.Current.Save();
        InventoryUI.Instance.RefreshUI();
        EquipmentUI.Instance.RefreshUI();
    }


    // Returns the appropriate EquipmentSlot based on the item's type
    private EquipmentSlot GetEquipmentSlotByType(ItemSO.ItemType itemType)
    {
        var character = CharacterData.Current;
        return itemType switch
        {
            ItemSO.ItemType.Weapon => character.MainHand,
            ItemSO.ItemType.Shield => character.Offhand,
            ItemSO.ItemType.Ring => EquipmentUI.Instance.FindFirstAvailableRingSlot() != -1
                ? character.Rings[EquipmentUI.Instance.FindFirstAvailableRingSlot() - 2]
                : null,
            _ => null
        };
    }


    private void DropItem() => ExecuteAction($"🛠 Dropping item {selectedSlot.slotIndex}");
    public void DestroyItem()
    {
        InventorySlot slot = CharacterData.Current.Inventory[selectedSlot.slotIndex];

        if (slot.IsEmpty())
        {
            Debug.LogError("❌ No item to destroy.");
            return;
        }

        HideMenu();
        DestroyItemUI.Instance.OpenDestroyUI(selectedSlot.slotIndex, slot);
    }

    private void ExecuteAction(string logMessage)
    {
        Debug.Log(logMessage);
        HideMenu();
    }

    public void SplitItem()
    {
        InventorySlot slot = CharacterData.Current.Inventory[selectedSlot.slotIndex];
        if (slot.Quantity <= 1)
        {
            Debug.LogError("❌ Cannot split a stack of 1 item.");
            return;
        }

        HideMenu();
        int emptySlotIndex = CharacterData.Current.Inventory.FindIndex(s => s.IsEmpty());
        if (emptySlotIndex == -1)
        {
            Debug.LogError("❌ No empty inventory slot available for split.");
            return;
        }

        SplitItemUI.Instance.OpenSplitUI(slot, selectedSlot.slotIndex, emptySlotIndex);
    }

    public void HideMenu() => menuPanel.SetActive(false);
}

// ✅ Fixed Extension Method for Buttons
public static class ButtonExtensions
{
    public static void SetListener(this Button button, Action action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action());
    }
}
