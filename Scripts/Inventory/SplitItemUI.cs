using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SplitItemUI : MonoBehaviour
{
    public static SplitItemUI Instance { get; private set; }

    public GameObject splitPanel;
    public TextMeshProUGUI itemNameText;
    public TMP_InputField amountInput;
    public Button confirmButton, cancelButton;

    private int originalSlotIndex;
    private int newSlotIndex;
    private InventorySlot originalSlot;

    private void Awake()
    {
        Instance = this;
        splitPanel.SetActive(false);
    }

    public void OpenSplitUI(InventorySlot slot, int fromIndex, int toIndex)
    {
        InventorySlotContextMenu.Instance.HideMenu(); // Close context menu when splitting

        originalSlot = slot;
        originalSlotIndex = fromIndex;
        newSlotIndex = toIndex;

        itemNameText.text = $"Split {slot.Quantity}x {ItemDatabaseSO.Instance.GetItemById(slot.ItemID).itemName}";
        amountInput.text = "1";

        splitPanel.SetActive(true);

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(ConfirmSplit);

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(Cancel);
    }
    public void Cancel()
    {
        splitPanel.SetActive(false); // Hide UI when canceled
    }


    private void ConfirmSplit()
    {
        int splitAmount = Mathf.Clamp(int.Parse(amountInput.text), 1, originalSlot.Quantity - 1);

        // Reduce from original stack
        CharacterData.Current.Inventory[originalSlotIndex].Quantity -= splitAmount;

        // Add new stack
        CharacterData.Current.Inventory[newSlotIndex] = new InventorySlot()
        {
            ItemID = originalSlot.ItemID,
            Quantity = splitAmount,
            IsOccupied = true
        };

        Debug.Log($"✅ Split {splitAmount}x {originalSlot.ItemID} from Slot {originalSlotIndex} to {newSlotIndex}");

        // Save and Refresh UI
        CharacterData.Current.Save();
        InventoryUI.Instance.RefreshUI();
        splitPanel.SetActive(false);
    }
}
