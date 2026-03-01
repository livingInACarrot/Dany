using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Mirror;

/// <summary>
/// Менеджер лобби для мультиплеера.
/// Работает в связке с NetworkGameManager (вся сетевая логика там).
/// Отвечает только за UI лобби и отображение игроков.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject openRoomsPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject endGamePanel;

    [Header("Lobby UI")]
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button roomCodeButton;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TMP_Dropdown countryDropdown;

    [Header("Player Data")]
    public Player localPlayer = new(new("Россия"));
    private List<NetworkPlayer> networkPlayers = new List<NetworkPlayer>();
    private bool isPrivate = true;
    private string currentRoomCode = "";
    [SerializeField] private Color myColor;
    [SerializeField] private Color othersColor;

    private int localPlayerNumber = -1;
    private int currentHostNumber = -1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        HideAllPanelsExceptOne(mainMenuPanel);
    }

    public void ShowLobby()
    {
        HideAllPanelsExceptOne(lobbyPanel);
        UpdatePlayersList();
    }

    public void ShowGameEndScreen(bool dannyWins)
    {
        HideAllPanelsExceptOne(endGamePanel);
        
        var endText = endGamePanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (endText != null)
        {
            endText.text = dannyWins ? "Дэнни победил!" : "Личности победили!";
        }
    }

    /// <summary>
    /// Вызывается при миграции хоста
    /// </summary>
    public void OnHostMigrated(int newHostNumber)
    {
        currentHostNumber = newHostNumber;
        UpdatePlayersList();
        
        // Если новый хост - локальный игрок, даём ему права хоста
        if (newHostNumber == localPlayerNumber)
        {
            NetworkChat.Instance.AddSystemMessage("Вы стали хостом!");
            if (startGameButton != null)
                startGameButton.interactable = CheckAllReady();
        }
    }

    public void OnCreateRoomClick()
    {
        HideAllPanelsExceptOne(lobbyPanel);
        ConfirmCreateRoom(isPrivate);
    }

    public void OnJoinRoomClick()
    {
        HideAllPanelsExceptOne(lobbyPanel);
    }

    public void OnOpenRoomsClick()
    {
        HideAllPanelsExceptOne(openRoomsPanel);
        RefreshOpenRoomsList();
    }

    public void OnRoomCodeButtonClick()
    {
        GUIUtility.systemCopyBuffer = currentRoomCode;
        Debug.Log("Код скопирован в буфер обмена");
    }

    public void OnCountrySelected(int index)
    {
        string[] countries = { "Россия", "СНГ", "Европа", "Азия", "Америка", "Другое" };
        localPlayer.Data.Country = countries[index];
        
        // Отправляем на сервер обновление страны
        if (NetworkClient.active && localPlayerNumber >= 0)
        {
            // Здесь можно добавить команду на сервер для обновления страны
        }
    }

    public void ConfirmCreateRoom(bool isPrivate)
    {
        currentRoomCode = GenerateRoomCode();
        roomCodeText.text = currentRoomCode;

        CreateRoom(currentRoomCode, isPrivate);

        ShowLobby();

        localPlayer.IsHost = true;
        localPlayer.IsReady = false;

        UpdatePlayersList();

        startGameButton.interactable = false;
    }

    public void JoinRoomByCode(string code)
    {
        ShowLobby();
        if (TryJoinRoom(code))
        {
            currentRoomCode = code;

            ShowLobby();

            localPlayer.IsHost = false;
            localPlayer.IsReady = false;

            UpdatePlayersList();

            roomCodeText.text = code;

            NetworkChat.Instance?.AddSystemMessage($"Присоединились к комнате {code}");
        }
        else
        {
            NetworkChat.Instance?.AddSystemMessage("Не удалось присоединиться к комнате. Проверьте код.");
        }
    }

    /// <summary>
    /// Сюда ссылается кнопка "Готов"
    /// </summary>
    public void OnReadyClick()
    {
        localPlayer.IsReady = !localPlayer.IsReady;
        
        // Отправляем команду на сервер
        if (NetworkClient.active)
        {
            var localNetPlayer = GetLocalNetworkPlayer();
            if (localNetPlayer != null)
            {
                localNetPlayer.CmdSetReady(localPlayer.IsReady);
            }
        }
        
        UpdatePlayersList();
        CheckAllReady();
    }
    
    public void OnReadyClick(Toggle readyToggle)
    {
        localPlayer.IsReady = !localPlayer.IsReady;

        if(CheckAllReady() && IsLocalPlayerHost())
            startGameButton.interactable = true;
    }

    /// <summary>
    /// Сюда ссылается кнопка начала игры
    /// </summary>
    public void OnStartGameClick()
    {
        if (!IsLocalPlayerHost())
        {
            NetworkChat.Instance.AddSystemMessage("Только хост может начать игру!");
            return;
        }

        if (!CheckAllReady())
        {
            NetworkChat.Instance.AddSystemMessage("Не все игроки готовы!");
            return;
        }

        // Отправляем команду на сервер через NetworkPlayer
        if (NetworkClient.active)
        {
            var localNetPlayer = GetLocalNetworkPlayer();
            if (localNetPlayer != null)
            {
                localNetPlayer.CmdStartGame();
            }
        }
    }

    public void OnLeaveRoomClick()
    {
        if (NetworkClient.active)
        {
            NetworkClient.Disconnect();
        }
        
        currentRoomCode = "";
        networkPlayers.Clear();

        ShowMainMenu();

        NetworkChat.Instance.AddSystemMessage("Вы покинули комнату");
    }

    private void OnEnable()
    {
        NetworkPlayer.OnPlayerAdded += HandlePlayerAdded;
        NetworkPlayer.OnPlayerRemoved += HandlePlayerRemoved;
    }

    private void OnDisable()
    {
        NetworkPlayer.OnPlayerAdded -= HandlePlayerAdded;
        NetworkPlayer.OnPlayerRemoved -= HandlePlayerRemoved;
    }

    private void HandlePlayerAdded(NetworkPlayer netPlayer)
    {
        if (!networkPlayers.Contains(netPlayer))
        {
            networkPlayers.Add(netPlayer);
            
            if (netPlayer.isLocalPlayer)
            {
                localPlayerNumber = netPlayer.playerNumber;
            }
            
            UpdatePlayerListFromNetwork();
        }
    }

    private void HandlePlayerRemoved(NetworkPlayer netPlayer)
    {
        networkPlayers.Remove(netPlayer);
        UpdatePlayerListFromNetwork();
    }

    /// <summary>
    /// Обновляет список игроков из сети (вызывается из NetworkGameManager)
    /// </summary>
    public void UpdatePlayerListFromNetwork(List<Player> playersList)
    {
        UpdatePlayersListInternal(playersList);
    }

    private void UpdatePlayerListFromNetwork()
    {
        // Очищаем старый список и создаем новый из networkPlayers
        List<Player> playersList = new();
        foreach (var netPlayer in networkPlayers)
        {
            Player player = new Player(new PlayerData(netPlayer.playerCountry))
            {
                Number = netPlayer.playerNumber,
                IsReady = netPlayer.isReady,
                IsHost = netPlayer.isHost,
                IsDanny = netPlayer.isDanny,
                Role = netPlayer.role
            };
            playersList.Add(player);
        }
        playersList = playersList.OrderBy(p => p.Number).ToList();
        
        UpdatePlayersListInternal(playersList);
    }

    private void UpdatePlayersListInternal(List<Player> playersList)
    {
        if (playersContainer == null || playerEntryPrefab == null) return;

        foreach (Transform child in playersContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var player in playersList)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playersContainer);

            TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
            string hostMark = player.IsHost ? " (хост)" : "";
            nameText.text = $"{LocalizationManager.Instance.GetText("voice")} {player.Number}" + hostMark;

            Toggle readyToggle = entry.GetComponentInChildren<Toggle>();
            readyToggle.isOn = player.IsReady;
            readyToggle.interactable = (player.Number == localPlayerNumber);

            Image image = entry.GetComponent<Image>();
            if (player.Number == localPlayerNumber)
            {
                image.color = myColor;
            }
            else
            {
                image.color = othersColor;
            }
        }
        
        // Обновляем состояние кнопки старта
        if (startGameButton != null)
        {
            startGameButton.interactable = IsLocalPlayerHost() && CheckAllReady();
        }
    }

    private void UpdatePlayersList()
    {
        if (playersContainer == null || playerEntryPrefab == null) return;

        foreach (Transform child in playersContainer)
        {
            Destroy(child.gameObject);
        }

        // Показываем локального игрока и сетевых игроков
        List<Player> allPlayers = new List<Player>();
        
        // Добавляем локального игрока
        allPlayers.Add(localPlayer);
        
        // Добавляем сетевых игроков
        foreach (var netPlayer in networkPlayers)
        {
            if (!netPlayer.isLocalPlayer)
            {
                Player player = new Player(new PlayerData(netPlayer.playerCountry))
                {
                    Number = netPlayer.playerNumber,
                    IsReady = netPlayer.isReady,
                    IsHost = netPlayer.isHost,
                    IsDanny = netPlayer.isDanny,
                    Role = netPlayer.role
                };
                allPlayers.Add(player);
            }
        }

        foreach (var player in allPlayers)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playersContainer);

            TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
            string hostMark = player.IsHost ? " (хост)" : "";
            nameText.text = $"{LocalizationManager.Instance.GetText("voice")} {player.Number}" + hostMark;

            Toggle readyToggle = entry.GetComponentInChildren<Toggle>();
            readyToggle.isOn = player.IsReady;
            readyToggle.interactable = (player == localPlayer);

            Image image = entry.GetComponent<Image>();
            if (player == localPlayer)
            {
                image.color = myColor;
            }
            else
            {
                image.color = othersColor;
            }
        }
        
        if (startGameButton != null)
        {
            startGameButton.interactable = IsLocalPlayerHost() && CheckAllReady();
        }
    }

    private bool CheckAllReady()
    {
        if (networkPlayers.Count == 0)
            return false;
            
        foreach (var netPlayer in networkPlayers)
        {
            if (!netPlayer.isReady)
                return false;
        }
        
        return networkPlayers.Count >= (NetworkGameManager.Instance != null ? NetworkGameManager.Instance.minPlayers : 3);
    }

    private bool IsLocalPlayerHost()
    {
        if (NetworkGameManager.Instance != null)
        {
            return NetworkGameManager.Instance.IsHost(localPlayerNumber);
        }
        
        // Fallback: проверяем по флагу isHost у локального NetworkPlayer
        var localNetPlayer = GetLocalNetworkPlayer();
        return localNetPlayer != null && localNetPlayer.isHost;
    }

    private NetworkPlayer GetLocalNetworkPlayer()
    {
        return networkPlayers.FirstOrDefault(p => p.isLocalPlayer);
    }

    private string GenerateRoomCode()
    {
        const string chars = "0123456789";
        string code = "";
        for (int i = 0; i < 5; i++)
        {
            code += chars[Random.Range(0, chars.Length)];
        }
        return code;
    }

    private void CreateRoom(string code, bool isPrivate)
    {
        // В мультиплеере здесь создание комнаты на сервере
        Debug.Log($"Создана комната {code}, приватная: {isPrivate}");
    }

    private bool TryJoinRoom(string code)
    {
        // В мультиплеере здесь попытка присоединения
        Debug.Log($"Попытка присоединиться к комнате {code}");
        return true; // Для демо всегда успешно
    }

    private void RefreshOpenRoomsList()
    {
        // В мультиплеере здесь получение списка открытых комнат
        Debug.Log("Обновление списка открытых комнат");
    }

    private void HideAllPanelsExceptOne(GameObject panel)
    {
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        openRoomsPanel.SetActive(false);
        gamePanel.SetActive(false);
        endGamePanel.SetActive(false);

        panel.SetActive(true);
    }
}