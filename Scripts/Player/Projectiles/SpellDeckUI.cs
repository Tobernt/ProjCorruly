using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SpellDeckUI : MonoBehaviour
{
    public Transform container;
    public GameObject spellSlotPrefab;

    private List<GameObject> activeSlots = new();

    public void ShowDeck(List<ProjectileEffect> deck, int currentIndex)
    {
        foreach (var slot in activeSlots)
            Destroy(slot);
        activeSlots.Clear();

        if (container == null || spellSlotPrefab == null)
        {
            Debug.LogWarning("⚠️ SpellDeckUI is missing references.");
            return;
        }

        for (int i = 0; i < deck.Count; i++)
        {
            GameObject slot = Instantiate(spellSlotPrefab, container, false); // Safe & simple

            var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                text.text = deck[i].name;

                if (i == currentIndex)
                    text.color = Color.cyan;
                else if (deck[i].spellType == ProjectileEffect.SpellType.Modifier)
                    text.color = Color.yellow;
                else
                    text.color = Color.white;
            }

            activeSlots.Add(slot);
        }
    }
}