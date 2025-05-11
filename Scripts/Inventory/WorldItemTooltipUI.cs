using UnityEngine;
using TMPro;

public class WorldItemTooltipUI : MonoBehaviour
{
    public static WorldItemTooltipUI Instance;

    [Header("Tooltip Elements")]
    public Canvas tooltipCanvas;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;

    private Transform target;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    private void LateUpdate()
    {
        if (target == null || !tooltipCanvas.gameObject.activeSelf)
            return;

        // Position tooltip above the target
        Vector3 worldPos = target.position + Vector3.up * 1.5f;
        transform.position = worldPos;

        // Face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }

    public void Show(ItemSO item, Transform worldTarget)
    {
        if (item == null || worldTarget == null)
        {
            Hide();
            return;
        }

        target = worldTarget;
        itemNameText.text = item.itemName;
        itemDescText.text = item.itemDescription;
        tooltipCanvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        tooltipCanvas.gameObject.SetActive(false);
        target = null;
    }
}
