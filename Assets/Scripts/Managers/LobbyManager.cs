using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Главный UI-контроллер лобби и главного меню.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private GameObject openRoomsPanel;
    [SerializeField] private GameObject rolePanel;
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private GameObject chatPanel;

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

    private string _roomCode;
    private bool _isHost;

    private bool _pendingCreateRoom;
    private string _pendingJoinCode;
    private bool _reconnecting;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; } // Уничтожаем только компонент, не GameObject (лобби-панель)
    }

    private void Start()
    {
        SetupUI();
        SubscribeToEvents();
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        NetworkPlayer.OnPlayerAdded -= OnNetworkPlayerAdded;
        NetworkPlayer.OnPlayerRemoved -= OnNetworkPlayerRemoved;
        GameRoom.OnRoomListChanged -= OnRoomListChanged;
        GamePlayer.OnSpawned -= OnGamePlayerSpawned;
    }

    private void SetupUI()
    {
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        startGameButton.onClick.AddListener(OnStartGameClick);
        readyToggle.onValueChanged.AddListener(OnReadyToggleChanged);
        privateRoomToggle.onValueChanged.AddListener(OnPrivacyToggleChanged);
        //voiceVolumeSlider.onValueChanged.AddListener(v => VoiceChat.Instance?.SetOutputVolume(v));
        //micVolumeSlider.onValueChanged.AddListener(v => VoiceChat.Instance?.SetMicVolume(v));
    }

    private void SubscribeToEvents()
    {
        NetworkPlayer.OnPlayerAdded += OnNetworkPlayerAdded;
        NetworkPlayer.OnPlayerRemoved += OnNetworkPlayerRemoved;
        GameRoom.OnRoomListChanged += OnRoomListChanged;
        GamePlayer.OnSpawned += OnGamePlayerSpawned;
    }

    #region Управление панелями

    public void ShowMainMenu()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void ShowLobby()
    {
        HideAllPanels();
        lobbyPanel.SetActive(true);
        chatPanel.SetActive(true);
        RefreshRoomPanel();
    }

    public void ShowSettings() => settingsPanel.SetActive(true);
    public void ShowRules() => rulesPanel.SetActive(true);
    public void ShowOpenRooms() => openRoomsPanel.SetActive(true);

    public void ShowRolePanel(bool isDany)
    {
        rolePanel.GetComponent<RolePanelUI>().SetTexts(isDany);
        rolePanel.SetActive(true);
        Invoke(nameof(HideRolePanel), 5f);
    }
    private void HideRolePanel() => rolePanel.SetActive(false);

    public void ShowGameEndScreen(bool danyWins)
    {
        HideAllPanels();
        gameEndPanel.GetComponent<GameEndPanelUI>().SetText(danyWins);
        gameEndPanel.SetActive(true);
    }

    private void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        settingsPanel.SetActive(false);
        rulesPanel.SetActive(false);
        openRoomsPanel.SetActive(false);
        rolePanel.SetActive(false);
        gameEndPanel.SetActive(false);
        chatPanel.SetActive(false);
    }

    #endregion

    #region Обработка кнопок

    public void OnCreateRoomClick()
    {
        NetworkPlayer np = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (np != null)
        {
            np.CmdCreateRoom();
            return;
        }
        _pendingCreateRoom = true;
        _pendingJoinCode = null;
        Reconnect();
    }

    public void OnJoinByCodeClick()
    {
        string code = roomCodeInput.text?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length != 5) return;
        JoinRoom(code);
    }

    public void JoinRoom(string code)
    {
        NetworkPlayer np = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (np != null)
        {
            np.CmdJoinRoom(code);
            return;
        }
        _pendingJoinCode = code;
        _pendingCreateRoom = false;
        Reconnect();
    }

    private void Reconnect()
    {
        _reconnecting = true;
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        _reconnecting = false;
        NetworkManager.singleton.networkAddress = MirrorNetworkManager.SERVER_ADDRESS;
        NetworkManager.singleton.StartClient();
    }

    public void OnOpenRoomsClick()
    {
        if (!NetworkClient.isConnected) Reconnect();
        ShowOpenRooms();
    }
    public void OnSettingsClick() => ShowSettings();
    public void OnRulesClick() => ShowRules();
    public void OnExitClick() => Application.Quit();

    public void OnRoomCodeButtonClick()
    {
        GUIUtility.systemCopyBuffer = _roomCode;
        Debug.Log($"Код скопирован: {_roomCode}");
    }

    public void OnBackToMainMenuClick()
    {
        if (NetworkClient.active)
        {
            NetworkPlayer np = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (np != null && !string.IsNullOrEmpty(_roomCode))
                np.CmdLeaveRoom();
            NetworkManager.singleton.StopClient();
        }
        _roomCode = string.Empty;
        _isHost = false;
        ShowMainMenu();
    }

    private void OnStartGameClick()
    {
        if (_isHost) NetworkClient.localPlayer.GetComponent<NetworkPlayer>().CmdStartGame();
    }

    private void OnReadyToggleChanged(bool isReady)
    {
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdSetReady(isReady);
        UpdateStartButton();
    }

    private void OnPrivacyToggleChanged(bool isPrivate)
    {
        if (_isHost)
            NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdSetRoomPrivacy(isPrivate);
    }

    #endregion

    #region Сетевые обратные вызовы

    public void OnRoomCreated(string code)
    {
        _roomCode = code;
        _isHost = true;
        roomCodeText.text = code;
        privateRoomToggle.interactable = true;
        ShowLobby();
    }

    public void OnJoinedRoom(string code)
    {
        _roomCode = code;
        _isHost = false;
        roomCodeText.text = code;
        privateRoomToggle.interactable = false;
        ShowLobby();
    }

    public void OnRoomError(string error)
    {
        NetworkChat.Instance?.AddSystemMessage($"Ошибка: {error}");
    }

    public void OnHostMigrated(uint newHostNetId)
    {
        if (NetworkClient.spawned.TryGetValue(newHostNetId, out NetworkIdentity id))
        {
            NetworkPlayer np = id.GetComponent<NetworkPlayer>();
            if (np != null && np.isLocalPlayer)
            {
                _isHost = true;
                privateRoomToggle.interactable = true;
                UpdateStartButton();
            }
        }
        NetworkChat.Instance?.AddSystemMessage("Хост комнаты изменился.");
    }

    public void OnGameStarted()
    {
        HideAllPanels();
        gameplayPanel.SetActive(true);
        chatPanel.SetActive(true);
    }

    public void OnRoleAssigned(bool isDanny)
    {
        ShowRolePanel(isDanny);
    }

    public void OnDisconnected()
    {
        if (_reconnecting) return; // StopClient внутри Reconnect — не трогаем состояние
        _roomCode = string.Empty;
        _isHost = false;
        _pendingCreateRoom = false;
        _pendingJoinCode = null;
        ShowMainMenu();
    }

    public void RefreshRoomPanel()
    {
        UpdateStartButton();
        RefreshPlayerList();
    }

    #endregion

    #region Внутренние обработчики

    public void OnLocalPlayerSpawned()
    {
        if (_pendingCreateRoom)
        {
            _pendingCreateRoom = false;
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().CmdCreateRoom();
        }
        else if (!string.IsNullOrEmpty(_pendingJoinCode))
        {
            string code = _pendingJoinCode;
            _pendingJoinCode = null;
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().CmdJoinRoom(code);
        }
    }

    private void OnNetworkPlayerAdded(NetworkPlayer player) => RefreshPlayerList();

    private void OnNetworkPlayerRemoved(NetworkPlayer removed)
    {
        PlayerListUI.Instance?.RemovePlayer(removed);
        UpdateStartButton();
    }

    private void OnRoomListChanged() => RoomsListUI.Instance?.RefreshRoomList();

    private void OnGamePlayerSpawned(GamePlayer gp)
    {
        if (gp.isOwned)
            PlayerListUI.Instance?.RefreshForGame();
    }

    #endregion

    #region Вспомогательное

    private void UpdateStartButton()
    {
        if (!_isHost || string.IsNullOrEmpty(_roomCode))
        {
            startGameButton.interactable = false;
            return;
        }

        GameRoom room = GameRoom.All.Find(r => r.RoomCode == _roomCode);
        if (room == null)
        {
            foreach (var id in NetworkClient.spawned.Values)
            {
                GameRoom gr = id?.GetComponent<GameRoom>();
                if (gr != null && gr.RoomCode == _roomCode) { room = gr; break; }
            }
        }
        if (room == null || !room.CanStart) { startGameButton.interactable = false; return; }

        bool allReady = true;
        foreach (NetworkIdentity id in NetworkClient.spawned.Values)
        {
            NetworkPlayer np = id?.GetComponent<NetworkPlayer>();
            if (np != null && np.CurrentRoomCode == _roomCode && !np.IsReady)
            { allReady = false; break; }
        }
        startGameButton.interactable = allReady;
    }

    private void RefreshPlayerList()
    {
        if (string.IsNullOrEmpty(_roomCode)) return;
        var players = new List<NetworkPlayer>();
        foreach (NetworkIdentity id in NetworkClient.spawned.Values)
        {
            NetworkPlayer np = id?.GetComponent<NetworkPlayer>();
            if (np != null && np.CurrentRoomCode == _roomCode)
                players.Add(np);
        }
        PlayerListUI.Instance.UpdatePlayerList(players);
    }

    private void OnLanguageChanged(int index)
    {
        LocalizationManager.Instance.SetLocale(index);
        PlayerPrefs.SetString("Language", LocalizationManager.Instance.GetStringLocale());
    }

    #endregion
}
