using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; } // Add Singleton Instance

    public GameObject inventoryPanel;
    public Transform inventorySlotsContainer;
    public GameObject inventorySlotPrefab;

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private bool isInventoryOpen = false;

    private void Awake()
    {
        Instance = this; // Assign instance when scene loads
    }

    private void Start()
    {
        // Ensure inventory loads when UI starts
        if (CharacterData.Current != null && CharacterData.Current.Inventory != null)
        {
            InitializeInventory(CharacterData.Current.Inventory.Count);
            RefreshUI();
        }
        else
        {
            Debug.LogError("❌ No character data found when initializing Inventory UI.");
        }
    }

    public void InitializeInventory(int size)
    {
        foreach (Transform child in inventorySlotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        if (size <= 0)
        {
            Debug.LogError("❌ Inventory size is invalid. Ensure the character data is loaded correctly.");
            return;
        }

        Debug.Log($"🛠 Creating {size} inventory slots...");

        for (int i = 0; i < size; i++)
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventorySlotsContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(i, this);
                slotUIs.Add(slotUI);
            }
            else
            {
                Debug.LogError("❌ InventorySlotUI component missing from slot prefab.");
            }
        }

        Debug.Log($"✅ Initialized {size} inventory slots.");
    }

    public void RefreshUI()
    {
        if (CharacterData.Current == null || CharacterData.Current.Inventory == null)
        {
            Debug.LogError("❌ Attempted to refresh inventory, but no character data found.");
            return;
        }

        Debug.Log($"🔄 Refreshing Inventory UI ({slotUIs.Count} slots)...");

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < CharacterData.Current.Inventory.Count)
            {
                slotUIs[i].UpdateSlot(CharacterData.Current.Inventory[i]);
            }
            else
            {
                slotUIs[i].UpdateSlot(null); // Force clear if beyond inventory count
            }
        }

        EquipmentUI.Instance?.RefreshUI();
    }



    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    public void SwapItems(int slotIndex1, int slotIndex2)
    {
        if (CharacterData.Current == null || CharacterData.Current.Inventory == null) return;

        InventorySlot slot1 = CharacterData.Current.Inventory[slotIndex1];
        InventorySlot slot2 = CharacterData.Current.Inventory[slotIndex2];

        // If dragging onto an identical stack, try to merge
        if (!slot1.IsEmpty() && !slot2.IsEmpty() && slot1.ItemID == slot2.ItemID)
        {
            ItemSO item = ItemDatabaseSO.Instance.GetItemById(slot1.ItemID);
            if (item != null && item.isStackable)
            {
                int maxStackSize = item.maxStackSize;
                int totalQuantity = slot1.Quantity + slot2.Quantity;

                if (totalQuantity <= maxStackSize)
                {
                    // Merge completely
                    slot2.Quantity = totalQuantity;
                    slot1.ClearItem(); // Clear the dragged stack
                }
                else
                {
                    // Partial merge (fill slot2, leave excess in slot1)
                    slot2.Quantity = maxStackSize;
                    slot1.Quantity = totalQuantity - maxStackSize;
                }

                CharacterData.Current.Save();
                RefreshUI();
                return;
            }
        }

        // If items are different or non-stackable, swap normally
        InventorySlot temp = slot1;
        CharacterData.Current.Inventory[slotIndex1] = slot2;
        CharacterData.Current.Inventory[slotIndex2] = temp;

        CharacterData.Current.Save();
        RefreshUI();
    }


    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);

        // Ensure UI updates when inventory opens
        if (isInventoryOpen)
        {
            RefreshUI();
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click anywhere
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                InventorySlotContextMenu.Instance.HideMenu();
            }
        }
    }
}
