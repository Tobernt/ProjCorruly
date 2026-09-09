using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    public GameObject tooltipPanel;
    public TextMeshProUGUI itemNameText, itemRarityText, itemDescriptionText, itemTypeText, itemStatsText;

    private Coroutine tooltipCoroutine;
    private Vector3 offset = new Vector3(20f, -20f, 0f); // Small offset from mouse

    private RectTransform tooltipRect;
    private RectTransform canvasRect;

    private void Awake()
    {
        Instance = this;
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        canvasRect = tooltipPanel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        if (tooltipPanel == null)
        {
            Debug.LogError("❌ ItemTooltipUI: tooltipPanel is missing in the Inspector!");
        }
        else
        {
            tooltipPanel.SetActive(false); // Ensure it's disabled at start
        }
    }

    public void ShowTooltip(ItemSO item, Vector3 mousePosition)
    {
        if (item == null || tooltipPanel == null) return;

        tooltipPanel.SetActive(true);

        if (tooltipCoroutine != null)
            StopCoroutine(tooltipCoroutine);

        tooltipCoroutine = StartCoroutine(DelayedShow(item, mousePosition));
    }

    IEnumerator DelayedShow(ItemSO item, Vector3 mousePosition)
    {
        yield return new WaitForSeconds(0.1f); // Slight delay to prevent flickering

        // Convert screen position to UI position
        Vector2 anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePosition, null, out anchoredPosition);

        // Apply offset
        tooltipRect.anchoredPosition = anchoredPosition + (Vector2)offset;

        // Set Tooltip Text
        itemNameText.text = item.itemName;
        itemRarityText.text = $"<i>{item.rarity}</i>";
        itemDescriptionText.text = item.itemDescription;
        itemTypeText.text = $"Type: {item.itemType}";

        // Apply rarity colors
        itemRarityText.color = GetRarityColor(item.rarity);

        // Set stats text
        string stats = "";
        if (item.damage > 0) stats += $"Damage: {item.damage}\n";
        if (item.attackSpeed > 0) stats += $"Attack Speed: {item.attackSpeed}\n";
        if (item.defense > 0) stats += $"Defense: {item.defense}\n";
        itemStatsText.text = stats;
        itemStatsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(stats));
    }

    public void HideTooltip()
    {
        if (tooltipCoroutine != null)
            StopCoroutine(tooltipCoroutine);
        tooltipPanel.SetActive(false);
    }

    private Color GetRarityColor(ItemSO.RarityType rarity)
    {
        switch (rarity)
        {
            case ItemSO.RarityType.Common: return Color.gray;
            case ItemSO.RarityType.Uncommon: return Color.green;
            case ItemSO.RarityType.Rare: return Color.blue;
            case ItemSO.RarityType.Epic: return new Color(0.5f, 0, 0.5f); // Purple
            case ItemSO.RarityType.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }
}
