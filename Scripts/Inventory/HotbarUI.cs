using UnityEngine;
using System.Collections.Generic;

public class HotbarUI : MonoBehaviour
{
    [Tooltip("Parent container for hotbar slots")]
    public Transform hotbarSlotsContainer;

    [Tooltip("Prefab for a hotbar slot (should use InventorySlotUI)")]
    public GameObject hotbarSlotPrefab;

    public List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private Hotbar hotbar;

    public void Initialize(Hotbar assignedHotbar)
    {
        hotbar = assignedHotbar;
        BuildSlots();
    }

    private void Start()
    {
        if (slotUIs.Count == 0)
        {
            hotbar = FindObjectOfType<Hotbar>(); // fallback if not set
            BuildSlots();
        }
    }

    private void BuildSlots()
    {
        if (hotbarSlotsContainer == null || hotbarSlotPrefab == null)
        {
            Debug.LogError("❌ Hotbar UI is missing required references.");
            return;
        }

        // Prevent duplicates
        foreach (Transform child in hotbarSlotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        for (int i = 0; i < 9; i++)
        {
            GameObject slotObj = Instantiate(hotbarSlotPrefab, hotbarSlotsContainer);
            InventorySlotUI ui = slotObj.GetComponent<InventorySlotUI>();
            if (ui != null)
            {
                ui.Setup(i, null, hotbar);
                slotUIs.Add(ui);
            }
            else
            {
                Debug.LogError("❌ InventorySlotUI component missing on hotbar prefab!");
            }
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (hotbar == null || CharacterData.Current == null || CharacterData.Current.Inventory == null) return;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            int invIndex = hotbar.hotbarIndices[i];
            if (invIndex >= 0 && invIndex < CharacterData.Current.Inventory.Count)
            {
                var slot = CharacterData.Current.Inventory[invIndex];
                slotUIs[i].UpdateSlot(slot);
            }
            else
            {
                slotUIs[i].ClearSlot();
            }

            slotUIs[i].SetSelected(i == hotbar.selectedIndex);
        }
    }
}
