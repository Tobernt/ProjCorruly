using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image icon;
    public TextMeshProUGUI quantityText;
    public int slotIndex { get; private set; }
    private InventoryUI inventoryUI;

    public void Setup(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;

        if (CharacterData.Current?.Inventory == null || slotIndex >= CharacterData.Current.Inventory.Count)
        {
            Debug.LogError($"❌ InventorySlotUI[{slotIndex}] is out of range or Inventory is null!");
            return;
        }

        UpdateSlot(CharacterData.Current.Inventory[slotIndex]);
    }

    public void UpdateSlot(InventorySlot slot)
    {
        if (slot == null)
        {
            Debug.LogError($"❌ InventorySlotUI[{slotIndex}] received a null InventorySlot.");
            return;
        }

        if (!slot.IsEmpty())
        {
            ItemSO item = ItemDatabaseSO.Instance?.GetItemById(slot.ItemID);
            if (item != null)
            {
                icon.sprite = item.itemIcon;
                icon.enabled = true;
                quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : "";
                quantityText.enabled = slot.Quantity > 1;
            }
            else
            {
                Debug.LogWarning($"⚠ Item ID {slot.ItemID} not found in ItemDatabase.");
                icon.enabled = false;
                quantityText.enabled = false;
            }
        }
        else
        {
            icon.enabled = false;
            quantityText.text = "";
            quantityText.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CharacterData.Current == null || CharacterData.Current.Inventory == null)
        {
            Debug.LogError("❌ CharacterData.Current or Inventory is null when hovering over slot.");
            return;
        }

        InventorySlot slot = CharacterData.Current.Inventory[slotIndex];
        if (slot == null || slot.IsEmpty()) return;

        ItemSO item = ItemDatabaseSO.Instance?.GetItemById(slot.ItemID);
        if (item != null)
        {
            if (ItemTooltipUI.Instance != null)
            {
                //Debug.Log($"🛠 Showing tooltip for {item.itemName} at position {Input.mousePosition}");
                ItemTooltipUI.Instance.ShowTooltip(item, Input.mousePosition);
            }
            else
            {
                Debug.LogError("❌ ItemTooltipUI.Instance is null!");
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.HideTooltip();
        }
        else
        {
            Debug.LogError("❌ ItemTooltipUI.Instance is null on exit!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (InventorySlotContextMenu.Instance != null)
            {
                InventorySlotContextMenu.Instance.ShowMenu(this);
            }
            else
            {
                Debug.LogError("❌ InventorySlotContextMenu.Instance is null on right-click!");
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CharacterData.Current.Inventory[slotIndex].IsEmpty()) return;
        icon.transform.SetParent(inventoryUI.transform);
        icon.raycastTarget = false;
        ItemTooltipUI.Instance?.HideTooltip(); // ✅ Safe Null Check
    }

    public void OnDrag(PointerEventData eventData)
    {
        icon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        icon.transform.SetParent(transform);
        icon.transform.localPosition = Vector3.zero;
        icon.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        if (draggedSlot != null && draggedSlot.slotIndex != slotIndex)
        {
            inventoryUI.SwapItems(draggedSlot.slotIndex, slotIndex);
        }
    }
}
