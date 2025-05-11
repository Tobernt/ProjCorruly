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
    public GameObject highlight; // assign via prefab, disabled by default
    private Hotbar hotbar;
    private HotbarUI hotbarUI;

    public void Setup(int index, InventoryUI ui, Hotbar hotbarRef = null)
    {
        slotIndex = index;
        inventoryUI = ui;
        hotbar = hotbarRef;

        if (CharacterData.Current?.Inventory == null || slotIndex >= CharacterData.Current.Inventory.Count)
        {
            Debug.LogError($"❌ InventorySlotUI[{slotIndex}] is out of range or Inventory is null!");
            return;
        }

        if (ui != null)
            UpdateSlot(CharacterData.Current.Inventory[slotIndex]);
        else if (hotbarRef != null && slotIndex < hotbarRef.hotbarIndices.Count)
        {
            int invIndex = hotbarRef.hotbarIndices[slotIndex];
            if (invIndex >= 0 && invIndex < CharacterData.Current.Inventory.Count)
                UpdateSlot(CharacterData.Current.Inventory[invIndex]);
            else
                ClearSlot();
        }
    }


    public void UpdateSlot(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty())
        {
            ClearSlot(); // ✅ Clear visuals if no item
            return;
        }

        ItemSO item = ItemDatabaseSO.Instance?.GetItemById(slot.ItemID);
        if (item != null)
        {
            icon.sprite = item.itemIcon;
            icon.enabled = true;
            quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : "";
            quantityText.enabled = slot.Quantity > 1;
            icon.color = Color.white;
            quantityText.color = Color.white;

        }
        else
        {
            Debug.LogWarning($"⚠ Item ID {slot.ItemID} not found in ItemDatabase.");
            ClearSlot();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CharacterData.Current == null || CharacterData.Current.Inventory == null)
            return;

        int inventoryIndex = -1;
        if (inventoryUI != null)
        {
            inventoryIndex = slotIndex;
        }
        else if (hotbar != null && slotIndex < hotbar.hotbarIndices.Count)
        {
            inventoryIndex = hotbar.hotbarIndices[slotIndex];
        }


        if (inventoryIndex < 0 || inventoryIndex >= CharacterData.Current.Inventory.Count)
            return;

        InventorySlot slot = CharacterData.Current.Inventory[inventoryIndex];
        if (slot == null || slot.IsEmpty())
            return;

        ItemSO item = ItemDatabaseSO.Instance?.GetItemById(slot.ItemID);
        if (item == null || string.IsNullOrEmpty(item.itemName))
            return;

        ItemTooltipUI.Instance?.ShowTooltip(item, Input.mousePosition);
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
            // 🔸 If this is a hotbar slot, clear it — do NOT open context menu
            if (inventoryUI == null)
            {
                hotbar?.ClearSlot(slotIndex);
                Debug.Log($"🧹 Hotbar slot {slotIndex} cleared via right-click.");
                return;
            }

            // 🔸 Otherwise, show the context menu for inventory items
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
        int inventoryIndex = inventoryUI != null ? slotIndex :
            (hotbar != null && slotIndex < hotbar.hotbarIndices.Count ? hotbar.hotbarIndices[slotIndex] : -1);

        if (inventoryIndex < 0 || inventoryIndex >= CharacterData.Current.Inventory.Count)
            return;

        var slot = CharacterData.Current.Inventory[inventoryIndex];
        if (slot == null || slot.IsEmpty()) return;

        Sprite itemIcon = icon?.sprite;
        if (itemIcon != null && DragIconUI.Instance != null)
        {
            DragIconUI.Instance.Show(itemIcon);
            DragIconUI.Instance.SetPosition(eventData.position);
        }

        ItemTooltipUI.Instance?.HideTooltip();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DragIconUI.Instance != null)
        {
            DragIconUI.Instance.SetPosition(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DragIconUI.Instance != null)
        {
            DragIconUI.Instance.Hide();
        }

        // ⬇️ Continue existing hotbar cleanup logic
        int hotbarIndex = hotbar?.hotbarIndices.FindIndex(i => i == slotIndex) ?? -1;
        if (hotbarIndex != -1)
        {
            if (eventData.pointerEnter == null ||
                eventData.pointerEnter.GetComponent<InventorySlotUI>() == null)
            {
                hotbar?.ClearSlot(hotbarIndex);
                hotbarUI?.RefreshUI();
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        if (draggedSlot == null || draggedSlot.slotIndex == slotIndex) return;

        if (inventoryUI != null)
        {
            // Inventory to inventory
            inventoryUI.SwapItems(draggedSlot.slotIndex, slotIndex);
        }
        else if (hotbar != null)
        {
            if (slotIndex >= 0 && slotIndex < hotbar.hotbarIndices.Count)
            {
                // Get the inventory index from the dragged slot
                int draggedInventoryIndex = -1;

                if (draggedSlot.inventoryUI != null)
                {
                    draggedInventoryIndex = draggedSlot.slotIndex;
                }
                else if (draggedSlot.hotbar != null &&
                         draggedSlot.slotIndex < draggedSlot.hotbar.hotbarIndices.Count)
                {
                    draggedInventoryIndex = draggedSlot.hotbar.hotbarIndices[draggedSlot.slotIndex];
                }

                // Only proceed if valid
                if (draggedInventoryIndex >= 0)
                {
                    // Assign the new slot
                    hotbar.AssignSlot(slotIndex, draggedInventoryIndex);

                    // 🔁 Clear the old hotbar slot (to avoid duplicates)
                    if (draggedSlot.hotbar != null)
                    {
                        int oldIndex = draggedSlot.slotIndex;
                        draggedSlot.hotbar.ClearSlot(oldIndex);
                        draggedSlot.ClearSlot(); // 💡 Clear visuals on drag source
                    }

                    hotbarUI?.RefreshUI();
                }
            }
            else
            {
                Debug.LogWarning($"❌ Invalid hotbar slot index {slotIndex}");
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null)
            highlight.SetActive(selected);
    }

    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;
        quantityText.text = "";
        quantityText.enabled = false;
        if (icon != null)
        {
            icon.color = new Color(1, 1, 1, 0); // fully transparent
        }

        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.color = new Color(1, 1, 1, 0); // fully transparent
        }
    }
}
