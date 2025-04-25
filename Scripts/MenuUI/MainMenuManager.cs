using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    public enum LaunchMode
    {
        None,
        Host,
        Join,
        Server
    }

    public LaunchMode CurrentMode { get; private set; } = LaunchMode.None;

    public GameObject mainMenuPanel;
    public GameObject characterSelectPanel;
    public GameObject optionsPanel;

    public Button hostButton;
    public Button joinButton;
    public Button serverButton;
    public Button optionsButton;
    public Button exitButton;

    public Button backFromCharacterButton;
    public Button backFromOptionsButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        hostButton.onClick.AddListener(() => OpenCharacterSelect(LaunchMode.Host));
        joinButton.onClick.AddListener(() => OpenCharacterSelect(LaunchMode.Join));
        serverButton.onClick.AddListener(StartServer);
        optionsButton.onClick.AddListener(OpenOptions);
        exitButton.onClick.AddListener(QuitGame);

        backFromCharacterButton.onClick.AddListener(ReturnToMainMenu);
        backFromOptionsButton.onClick.AddListener(ReturnToMainMenu);

        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        characterSelectPanel.SetActive(false);
        optionsPanel.SetActive(false);
        CurrentMode = LaunchMode.None;
    }

    private void OpenCharacterSelect(LaunchMode mode)
    {
        CurrentMode = mode;
        mainMenuPanel.SetActive(false);
        characterSelectPanel.SetActive(true);
        optionsPanel.SetActive(false);

        Debug.Log($"✅ Entered Character Select in {mode} mode.");
    }

    private void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    private void ReturnToMainMenu()
    {
        ShowMainMenu();
    }

    public void StartGameBasedOnMode()
    {
        if (CharacterData.Current == null)
        {
            Debug.LogError("❌ No character loaded!");
            return;
        }

        Debug.Log($"🚀 Starting game with {CharacterData.Current.Name} as {CurrentMode}");

        switch (CurrentMode)
        {
            case LaunchMode.Host:
                NetworkManager.singleton.StartHost();
                break;
            case LaunchMode.Join:
                NetworkManager.singleton.StartClient();
                break;
            case LaunchMode.Server:
                NetworkManager.singleton.StartServer();
                break;
            default:
                Debug.LogWarning("⚠ Invalid mode! No action taken.");
                break;
        }
    }

    private void StartServer()
    {
        CurrentMode = LaunchMode.Server;
        Debug.Log("🖥 Starting server-only instance...");
        NetworkManager.singleton.StartServer();
    }

    private void QuitGame()
    {
        Debug.Log("👋 Quitting game...");
        Application.Quit();
    }
}
