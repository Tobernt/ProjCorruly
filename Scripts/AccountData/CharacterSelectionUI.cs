using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    public Transform characterListContainer;
    public GameObject characterSlotPrefab;
    public InputField characterNameInput;
    public Button createButton, deleteButton, loadButton;

    private List<CharacterSlot> characterSlots = new List<CharacterSlot>();

    private void Start()
    {
        createButton.onClick.AddListener(CreateCharacter);
        loadButton.onClick.AddListener(LoadSelectedCharacter);
        deleteButton.onClick.AddListener(DeleteSelectedCharacter);
        RefreshCharacterList();
    }

    // ✅ Load all characters into UI
    private void RefreshCharacterList()
    {
        // Clear existing slots
        foreach (Transform child in characterListContainer)
            Destroy(child.gameObject);

        characterSlots.Clear();

        List<CharacterData> characters = CharacterData.GetAllCharacters();
        foreach (CharacterData character in characters)
        {
            GameObject slotObj = Instantiate(characterSlotPrefab, characterListContainer);
            CharacterSlot slot = slotObj.GetComponent<CharacterSlot>();
            slot.Initialize(character, this);
            characterSlots.Add(slot);
        }

        Debug.Log($"✅ Loaded {characters.Count} characters.");
    }

    // ✅ Create new character with full inventory & equipment slots
    private void CreateCharacter()
    {
        string name = characterNameInput.text;
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogError("❌ Character name cannot be empty!");
            return;
        }

        CharacterData newCharacter = new CharacterData(name, 1, 100);
        newCharacter.Save();
        RefreshCharacterList();
    }

    private void LoadSelectedCharacter()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            string selectedName = PlayerPrefs.GetString("SelectedCharacter");
            Debug.Log($"🛠 Attempting to load character: {selectedName}");

            CharacterData loadedCharacter = CharacterData.Load(selectedName);
            if (loadedCharacter == null)
            {
                Debug.LogError($"❌ Failed to load character: {selectedName}. CharacterData.Load() returned null.");
                return;
            }

            // ✅ Store character in memory only, no UI initialization here
            CharacterData.Current = loadedCharacter;
            Debug.Log($"✅ Character {selectedName} stored in memory. Waiting for player to initialize.");
        }
        else
        {
            Debug.LogError("❌ No character selected in PlayerPrefs!");
        }
    }



    // ✅ Delete selected character
    private void DeleteSelectedCharacter()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            string selectedName = PlayerPrefs.GetString("SelectedCharacter");
            CharacterData.DeleteCharacter(selectedName);
            RefreshCharacterList();
            PlayerPrefs.DeleteKey("SelectedCharacter");
            Debug.Log($"🗑️ Deleted character: {selectedName}");
        }
        else
        {
            Debug.LogError("❌ No character selected to delete!");
        }
    }

    // ✅ Set selected character in PlayerPrefs
    public void SelectCharacter(string characterName)
    {
        PlayerPrefs.SetString("SelectedCharacter", characterName);
        Debug.Log($"✅ Selected character: {characterName}");
    }
}
