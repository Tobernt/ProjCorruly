using UnityEngine;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    public Text characterNameText;
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

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() => ui.SelectCharacter(character.Name));
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
