using UnityEngine;
using UnityEngine.UI; // Or use TMPro;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI healthText;


    public void SetHealth(int current, int max)
    {
        if (healthText != null)
        {
            healthText.text = $"{current} / {max}";
        }
    }
}
