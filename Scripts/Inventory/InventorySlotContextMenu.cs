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
        HideMenu();
    }
    private void UseItem()
    {
        InventorySlot slot = CharacterData.Current.Inventory[selectedSlot.slotIndex];
        if (slot.IsEmpty())
        {
            Debug.LogError("❌ No item to use.");
            return;
        }

        ItemSO item = ItemDatabaseSO.Instance.GetItemById(slot.ItemID);
        if (item == null)
        {
            Debug.LogError($"❌ Cannot use item: Item ID {slot.ItemID} not found.");
            return;
        }

        Debug.Log($"🛠 Using item: {item.itemName}");
        // Your actual use logic goes here...

        if (slot.Quantity <= 0) slot.ClearItem();

        CharacterData.Current.Save();
        InventoryUI.Instance.RefreshUI();
    }

    private void EquipItem(ItemSO item, int inventoryIndex)
    {
        InventorySlot inventorySlot = CharacterData.Current.Inventory[inventoryIndex];
        EquipmentSlot equipmentSlot = GetTargetEquipmentSlot(item);

        if (equipmentSlot == null)
        {
            Debug.LogError("❌ No valid equipment slot found.");
            return;
        }

        equipmentSlot.EquipItem(inventorySlot.ItemID);
        inventorySlot.ClearItem();

        CharacterData.Current.Save();
        InventoryUI.Instance.RefreshUI();
        EquipmentUI.Instance.RefreshUI();
    }

    private EquipmentSlot GetTargetEquipmentSlot(ItemSO item)
    {
        if (item.itemType == ItemSO.ItemType.Ring)
        {
            int uiIndex = EquipmentUI.Instance.FindFirstAvailableRingSlot();
            return uiIndex != -1 ? CharacterData.Current.Rings[uiIndex - 2] : null;
        }

        return item.itemType switch
        {
            ItemSO.ItemType.Weapon => CharacterData.Current.MainHand,
            ItemSO.ItemType.Shield => CharacterData.Current.Offhand,
            _ => null
        };
    }

    private EquipmentSlot GetEquipmentSlotByType(ItemSO.ItemType itemType)
    {
        var character = CharacterData.Current;
        return itemType switch
        {
            ItemSO.ItemType.Weapon => character.MainHand,
            ItemSO.ItemType.Shield => character.Offhand,
            _ => null
        };
    }
    private void DropItem()
    {
        ExecuteAction($"🛠 Dropping item {selectedSlot.slotIndex}");
        // Implement actual drop logic here if needed
    }

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
    public void HideMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }
}


// ✅ Extension method for cleaner button listener assignment
public static class ButtonExtensions
{
    public static void SetListener(this Button button, Action action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action());
    }
}
