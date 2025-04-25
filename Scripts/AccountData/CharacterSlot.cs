using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSlot : MonoBehaviour
{
    public TextMeshProUGUI characterNameText;
    private string characterFileName;

    public void Initialize(CharacterData character, CharacterSelectionUI ui)
    {
        if (character == null)
        {
            Debug.LogError("❌ CharacterSlot received a null CharacterData!");
            return;
        }

        characterNameText.text = character.Name;
        characterFileName = character.Name;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ui.SelectCharacter(character.Name));
        }
        else
        {
            Debug.LogWarning("⚠ No Button component found on CharacterSlot.");
        }
    }

    public void OnSelectCharacter()
    {
        PlayerPrefs.SetString("SelectedCharacter", characterFileName);
        Debug.Log($"✅ Selected Character: {characterFileName}");
    }

    public void OnDeleteCharacter()
    {
        CharacterData.DeleteCharacter(characterFileName);
        Destroy(gameObject);
        Debug.Log($"🗑️ Character {characterFileName} deleted!");
    }
}
