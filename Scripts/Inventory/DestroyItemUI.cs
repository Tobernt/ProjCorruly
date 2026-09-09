using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DestroyItemUI : MonoBehaviour
{
    public static DestroyItemUI Instance { get; private set; }

    public GameObject destroyPanel;
    public TextMeshProUGUI itemNameText;
    public TMP_InputField amountInput;
    public Button confirmButton, cancelButton;

    private int slotIndex;
    private InventorySlot selectedSlot;

    private void Awake()
    {
        Instance = this;
        destroyPanel.SetActive(false);
    }

    public void OpenDestroyUI(int index, InventorySlot slot)
    {
        InventorySlotContextMenu.Instance.HideMenu(); // Close context menu when opening

        slotIndex = index;
        selectedSlot = slot;

        itemNameText.text = $"Destroy {slot.Quantity}x {ItemDatabaseSO.Instance.GetItemById(slot.ItemID).itemName}?";
        amountInput.text = "1";

        destroyPanel.SetActive(true);

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(ConfirmDestroy);

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(Cancel);
    }

    public void Cancel()
    {
        destroyPanel.SetActive(false); // Hide UI when canceled
    }

    private void ConfirmDestroy()
    {
        int destroyAmount = Mathf.Clamp(int.Parse(amountInput.text), 1, selectedSlot.Quantity);

        // Reduce or clear stack
        CharacterData.Current.Inventory[slotIndex].Quantity -= destroyAmount;
        if (CharacterData.Current.Inventory[slotIndex].Quantity <= 0)
        {
            CharacterData.Current.Inventory[slotIndex].ClearItem();
        }

        Debug.Log($"🔥 Destroyed {destroyAmount}x {selectedSlot.ItemID} from Slot {slotIndex}");

        // Save & Refresh UI
        CharacterData.Current.Save();
        InventoryUI.Instance.RefreshUI();
        destroyPanel.SetActive(false);
    }
}
