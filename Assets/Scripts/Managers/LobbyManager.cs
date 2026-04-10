using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private GameObject openRoomsPanel;
    [SerializeField] private GameObject rolePanel;
    [SerializeField] private GameObject gameEndPanel;

    [Header("Several-scenes objects")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private Button rulesButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Main Menu")]
    [SerializeField] private TMP_InputField roomCodeInput;

    [Header("Lobby")]
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Toggle readyToggle;
    [SerializeField] private Toggle privateRoomToggle;

    [Header("Settings")]
    [SerializeField] private Dropdown languageDropdown;
    [SerializeField] private Slider voiceVolumeSlider;
    [SerializeField] private Slider micVolumeSlider;
    [SerializeField] private Button bindPushToTalkButton;

    [Header("Rules")]
    [SerializeField] private Scrollbar rulesScrollbar;
    [SerializeField] private UnityEngine.UI.Image rulesImage;

    [Header("Game End")]
    [SerializeField] private TextMeshProUGUI winnerText;

    private string currentRoomCode = "";
    private bool isHost = false;
    private List<Player> playersList = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("Lobby Manager started");
        SetupUI();
        SubscribeToEvents();
        ShowMainMenu();
    }

    private void SetupUI()
    {
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        //voiceVolumeSlider.onValueChanged.AddListener(VoiceChatManager.Instance.SetVoiceVolume);
        //micVolumeSlider.onValueChanged.AddListener(VoiceChatManager.Instance.SetMicVolume);

        startGameButton.onClick.AddListener(OnStartGameClick);
        readyToggle.onValueChanged.AddListener(OnReadyToggleChanged);
        privateRoomToggle.onValueChanged.AddListener(OnPrivateRoomToggleChanged);

        rulesButton.onClick.AddListener(ShowRules);
        settingsButton.onClick.AddListener(ShowSettings);
        exitButton.onClick.AddListener(OnExitClick);
    }

    private void SubscribeToEvents()
    {
        NetworkPlayer.OnPlayerAdded += OnPlayerAdded;
        NetworkPlayer.OnPlayerRemoved += OnPlayerRemoved;
    }


    public void ShowMainMenu()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void ShowLobby()
    {
        HideAllPanels();
        lobbyPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void ShowRules()
    {
        rulesPanel.SetActive(true);
    }

    public void ShowOpenRooms()
    {
        HideAllPanels();
        if (openRoomsPanel != null)
            openRoomsPanel.SetActive(true);
    }

    public void ShowRolePanel(bool isDanny)
    {
        HideAllPanels();
        if (rolePanel != null)
        {
            rolePanel.SetActive(true);
            TextMeshProUGUI text = rolePanel.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = isDanny ? "Вы - Дэнни!" : "Вы - личность!";
            }
        }
        Invoke(nameof(HideRolePanel), 5f);
    }

    private void HideRolePanel()
    {
        rolePanel.SetActive(false);
    }

    public void ShowGameEndScreen(bool dannyWins)
    {
        HideAllPanels();
        if (gameEndPanel != null)
        {
            gameEndPanel.SetActive(true);
            if (winnerText != null)
            {
                winnerText.text = dannyWins ? "Дэнни победил!" : "Личности победили!";
            }
        }
    }

    private void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        settingsPanel.SetActive(false);
        rulesPanel.SetActive(false);
        openRoomsPanel.SetActive(false);
        rolePanel.SetActive(false);
        gameEndPanel.SetActive(false);
    }

    public void OnCreateRoomClick()
    {
        NetworkManager.singleton.StartHost();
        isHost = true;

        //Invoke(nameof(GenerateRoomCode), 0.5f);
        GenerateRoomCode();
        ShowLobby();
    }

    private void GenerateRoomCode()
    {
        currentRoomCode = Random.Range(10000, 99999).ToString();
        roomCodeText.text = currentRoomCode;
        Debug.Log($"Создана комната с кодом: {currentRoomCode}");
    }

    public void OnJoinByCodeClick()
    {
        if (string.IsNullOrWhiteSpace(roomCodeInput.text))
        {
            return;
        }
        
        // Добавить проверку кода комнаты
        NetworkManager.singleton.networkAddress = "localhost";
        NetworkManager.singleton.StartClient();

        ShowLobby();
    }

    public void OnOpenRoomsClick()
    {
        ShowOpenRooms();
    }

    public void OnSettingsClick()
    {
        ShowSettings();
    }

    public void OnRulesClick()
    {
        ShowRules();
    }

    public void OnExitClick()
    {
        UnityEngine.Application.Quit();
    }

    public void OnRoomCodeButtonClick()
    {
        GUIUtility.systemCopyBuffer = roomCodeText.text;

    }

    public void OnBackToMainMenuClick()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            NetworkManager.singleton.StopHost();
        }
        ShowMainMenu();
    }

    private void OnStartGameClick()
    {
        NetworkPlayer localPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
        if (localPlayer != null)
        {
            localPlayer.CmdStartGame();
        }
    }

    private void OnReadyToggleChanged(bool isReady)
    {
        NetworkPlayer localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer != null)
        {
            localPlayer.CmdSetReady(isReady);
        }
    }

    private void OnPrivateRoomToggleChanged(bool isPrivate)
    {
        // Добавить логику приватности комнаты
        Debug.Log($"Приватность комнаты изменена: {isPrivate}");
    }

    public void CopyRoomCode()
    {
        if (!string.IsNullOrEmpty(currentRoomCode))
        {
            GUIUtility.systemCopyBuffer = currentRoomCode;
            NetworkChat.Instance.AddSystemMessage("Код комнаты скопирован!");
        }
    }

    public void UpdatePlayerListFromNetwork(List<Player> players)
    {
        playersList = players;
        UpdateStartGameButton();
    }

    private void UpdateStartGameButton()
    {
        if (startGameButton == null) return;

        if (!isHost)
        {
            startGameButton.interactable = false;
            return;
        }

        int readyCount = 0;
        foreach (var player in playersList)
        {
            if (player.IsReady) readyCount++;
        }

        startGameButton.interactable = readyCount >= playersList.Count && playersList.Count >= 3;
    }

    private void OnPlayerAdded(NetworkPlayer player)
    {
        Debug.Log($"Игрок добавлен: {player.playerNumber}");
    }

    private void OnPlayerRemoved(NetworkPlayer player)
    {
        Debug.Log($"Игрок удалён: {player.playerNumber}");
    }

    public void OnHostMigrated(int newHostNumber)
    {
        NetworkChat.Instance.AddSystemMessage($"Хост передан игроку {newHostNumber}");
    }

    private void OnLanguageChanged(int index)
    {
        string[] languages = { "ru", "en", "fr", "es", "de", "ch" };
        if (index >= 0 && index < languages.Length)
        {
            PlayerPrefs.SetString("Language", languages[index]);
            LocalizationManager.Instance.SetLocale(index);
        }
    }
}