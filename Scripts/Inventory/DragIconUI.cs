using UnityEngine;
using UnityEngine.UI;

public class DragIconUI : MonoBehaviour
{
    public static DragIconUI Instance;
    public Image iconImage;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(Sprite icon)
    {
        if (iconImage == null) return;

        iconImage.sprite = icon;
        iconImage.enabled = true;
        iconImage.raycastTarget = false;
        gameObject.SetActive(true);
    }

    public void SetPosition(Vector2 screenPosition)
    {
        transform.position = screenPosition;
    }

    public void Hide()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
        gameObject.SetActive(false);
    }
}
