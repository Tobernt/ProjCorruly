using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image icon;
    public int slotIndex { get; private set; }
    private EquipmentSlot equipmentSlot;

    public void Setup(int index, EquipmentSlot slot)
    {
        slotIndex = index;
        equipmentSlot = slot;

        if (equipmentSlot == null)
        {
            Debug.LogError($"❌ EquipmentSlotUI[{slotIndex}] has no assigned EquipmentSlot! Check `GetEquipmentSlotByIndex()` in EquipmentUI.cs");
            return;
        }

        Debug.Log($"✅ EquipmentSlotUI[{slotIndex}] correctly assigned to {equipmentSlot.AllowedItemType}");
        UpdateSlot();
    }


    public void UpdateSlot()
    {
        if (icon == null)
        {
            Debug.LogWarning($"⚠ EquipmentSlotUI[{slotIndex}] icon is missing or destroyed.");
            return;
        }

        if (equipmentSlot == null)
        {
            Debug.Log($"⚠ EquipmentSlotUI[{slotIndex}] has no assigned EquipmentSlot. Skipping update.");
            icon.enabled = false;
            return;
        }

        if (!equipmentSlot.IsEmpty())
        {
            ItemSO item = ItemDatabaseSO.Instance?.GetItemById(equipmentSlot.ItemID);
            if (item != null)
            {
                icon.sprite = item.itemIcon;
                icon.enabled = true;
            }
            else
            {
                Debug.LogWarning($"⚠ Equipment slot {slotIndex} references an invalid item ID: {equipmentSlot.ItemID}");
                icon.enabled = false;
            }
        }
        else
        {
            icon.enabled = false;
        }
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log($"🛠 Right-click detected on EquipmentSlot[{slotIndex}]. Unequipping...");
            TryUnequipItem();
        }
    }

    private void TryUnequipItem()
    {
        if (equipmentSlot == null || equipmentSlot.IsEmpty()) return;

        int emptySlotIndex = CharacterData.Current.Inventory.FindIndex(s => s.IsEmpty());
        if (emptySlotIndex == -1)
        {
            Debug.LogWarning("⚠ No empty inventory slot available.");
            return;
        }

        equipmentSlot.UnequipItem(); // This already handles returning item to inventory

        InventoryUI.Instance.RefreshUI();
        EquipmentUI.Instance.RefreshUI();

        Debug.Log($"🛠 Unequipped item and moved to Inventory Slot {emptySlotIndex}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (equipmentSlot == null) return;
        if (equipmentSlot.IsEmpty()) return;

        ItemSO item = ItemDatabaseSO.Instance?.GetItemById(equipmentSlot.ItemID);
        if (item != null)
        {
            ItemTooltipUI.Instance?.ShowTooltip(item, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.HideTooltip();
    }
    public void OnClearSlot()
    {
        if (equipmentSlot == null)
        {
            Debug.LogWarning($"⚠ OnClearSlot called, but EquipmentSlotUI[{slotIndex}] has no assigned slot.");
            return;
        }

        Debug.Log($"🗑 Clearing EquipmentSlotUI[{slotIndex}]");
        equipmentSlot.UnequipItem();
        UpdateSlot();
    }
}
