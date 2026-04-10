using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject networkPlayerPrefab;

    [Header("Game Settings")]
    public int minPlayers = 3;
    public int maxPlayers = 8;

    [Header("Debug")]
    public bool debugMode = false;

    [SyncVar(hook = nameof(OnPhaseChanged))]
    public GamePhase currentPhase;

    [SyncVar]
    public int personalitiesScore;

    [SyncVar]
    public int dannyScore;

    [SyncVar]
    public int currentPlayerNumber;

    [SyncVar]
    public int dannyPlayerNumber;

    [SyncVar]
    public int hostPlayerNumber;

    public readonly SyncList<int> playersOrder = new SyncList<int>();

    private Dictionary<int, NetworkPlayer> players = new Dictionary<int, NetworkPlayer>();

    private IdeasCard currentIdeasCard;
    private int secretWordIndex;
    private readonly float discussionTime = 60f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        Debug.Log("Server started");
        base.OnStartServer();

        personalitiesScore = 0;
        dannyScore = 0;
        currentPhase = GamePhase.Lobby;
        hostPlayerNumber = 0;

        GameObject playerObj = Instantiate(networkPlayerPrefab);
        NetworkServer.Spawn(playerObj);
        NetworkPlayer player = playerObj.GetComponent<NetworkPlayer>();
        player.playerCountry = "Россия";

        UpdatePlayersList();

        if (debugMode)
        {
            Invoke(nameof(CreateDebugPlayers), 1f);
        }
    }

    private void CreateDebugPlayers()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject playerObj = Instantiate(networkPlayerPrefab);
            NetworkServer.Spawn(playerObj);

            NetworkPlayer player = playerObj.GetComponent<NetworkPlayer>();
            player.playerCountry = new string[] { "Россия", "СНГ", "Европа", "Азия" }[i];
        }
    }

    [Server]
    public void RegisterPlayer(NetworkPlayer player)
    {
        int playerNumber = player.playerNumber;
        players[playerNumber] = player;

        if (!playersOrder.Contains(playerNumber))
        {
            playersOrder.Add(playerNumber);
        }

        if (hostPlayerNumber == 0)
        {
            hostPlayerNumber = playerNumber;
            player.isHost = true;
            Debug.Log($"Игрок {playerNumber} стал хостом");
        }
        Debug.Log($"Игрок {playerNumber} зарегистрирован. Всего: {players.Count}");
        UpdatePlayersList();
    }

    [Server]
    public void RemovePlayer(int playerNumber)
    {
        if (!players.ContainsKey(playerNumber))
            return;

        bool wasHost = (playerNumber == hostPlayerNumber);

        players.Remove(playerNumber);
        playersOrder.Remove(playerNumber);

        Debug.Log($"Игрок {playerNumber} отключился. Осталось: {players.Count}");

        if (wasHost && playersOrder.Count > 0)
        {
            MigrateHost();
        }

        UpdatePlayersList();
    }
    [Server]
    private void MigrateHost()
    {
        if (playersOrder.Count == 0)
        {
            Debug.LogWarning("Нет игроков для передачи хоста");
            return;
        }

        int oldHostIndex = playersOrder.IndexOf(hostPlayerNumber);
        int newHostIndex = (oldHostIndex + 1) % playersOrder.Count;
        
        if (oldHostIndex < 0)
            newHostIndex = 0;

        hostPlayerNumber = playersOrder[newHostIndex];

        foreach (var kvp in players)
        {
            kvp.Value.isHost = (kvp.Key == hostPlayerNumber);
        }

        Debug.Log($"Хост передан игроку {hostPlayerNumber}");
        RpcHostMigrated(hostPlayerNumber);
    }

    [ClientRpc]
    private void RpcHostMigrated(int newHostNumber)
    {
        NetworkChat.Instance.AddSystemMessage($"Хост передан игроку {newHostNumber}");
        LobbyManager.Instance.OnHostMigrated(newHostNumber);
    }

    [Server]
    private void UpdatePlayersList()
    {
        List<PlayerData> playerDataList = new();
        foreach (var player in players.Values)
        {
            playerDataList.Add(new PlayerData(player.playerCountry));
        }
        RpcUpdatePlayersList(playerDataList.ToArray());
    }

    [ClientRpc]
    private void RpcUpdatePlayersList(PlayerData[] playerDataArray)
    {
        List<Player> playersList = new();
        foreach (var data in playerDataArray)
        {
            playersList.Add(new(data));
        }
        playersList = playersList.OrderBy(p => p.Number).ToList();

        PlayerListUI.Instance.UpdatePlayerList(playersList);
        LobbyManager.Instance.UpdatePlayerListFromNetwork(playersList);
    }

    [Server]
    public void StartNetworkGame()
    {
        if (players.Count < minPlayers)
        {
            RpcShowMessage($"Недостаточно игроков. Нужно минимум {minPlayers}.");
            return;
        }

        if (currentPhase != GamePhase.Lobby)
        {
            RpcShowMessage("Игра уже идёт!");
            return;
        }

        int dannyIndex = Random.Range(0, playersOrder.Count);
        dannyPlayerNumber = playersOrder[dannyIndex];

        foreach (var kvp in players)
        {
            NetworkPlayer player = kvp.Value;
            player.isDanny = (kvp.Key == dannyPlayerNumber);

            TargetSendRole(player.connectionToClient, player.isDanny);
        }

        currentPhase = GamePhase.RoleDistribution;
        StartNextTurn();
    }

    [TargetRpc]
    private void TargetSendRole(NetworkConnection conn, bool isDanny)
    {
        if (isDanny)
        {
            NetworkChat.Instance.AddSystemMessage("Вы - Дэни! Мешайте другим угадывать слово.");
        }
        else
        {
            NetworkChat.Instance.AddSystemMessage("Вы - личность! Помогайте угадывать слово.");
        }
    }

    [Server]
    private void StartNextTurn()
    {
        if (playersOrder.Count == 0) return;

        int currentIndex = playersOrder.IndexOf(currentPlayerNumber);

        if (currentIndex < 0)
        {
            currentPlayerNumber = playersOrder[0];
            currentIndex = 0;
        }
        else
        {
            int nextIndex = (currentIndex + 1) % playersOrder.Count;
            currentPlayerNumber = playersOrder[nextIndex];
        }
        SetTurnRoles(currentPlayerNumber);
        currentPhase = GamePhase.TurnInProgress;
        DrawCardsForPlayer(currentPlayerNumber);
        DrawIdeasCard();
    }

    [Server]
    private void SetTurnRoles(int activePlayerNumber)
    {
        int activeIndex = playersOrder.IndexOf(activePlayerNumber);
        int decisiveIndex = (activeIndex - 1 + playersOrder.Count) % playersOrder.Count;
        int decisivePlayerNumber = playersOrder[decisiveIndex];

        foreach (var kvp in players)
        {
            NetworkPlayer player = kvp.Value;

            if (kvp.Key == activePlayerNumber)
            {
                player.role = Role.Active;
            }
            else if (kvp.Key == decisivePlayerNumber)
            {
                player.role = Role.Decisive;
            }
            else
            {
                player.role = Role.Waiting;
            }
        }
    }

    [Server]
    private void DrawCardsForPlayer(int playerNumber)
    {
        if (!players.TryGetValue(playerNumber, out NetworkPlayer player))
            return;

        player.handCardNetIds.Clear();

        for (int i = 0; i < 7; i++)
        {
            CreateCardForPlayer(playerNumber);
        }
    }

    [Server]
    private void CreateCardForPlayer(int playerNumber)
    {
        if (!players.TryGetValue(playerNumber, out NetworkPlayer player))
            return;

        Sprite pic = PicturesDeck.Instance.DrawCard();
        if (pic == null) CheckGameEndConditions();

        GameObject cardObj = PlayingCardsTable.Instance.SpawnCardInHand();
        NetworkServer.Spawn(cardObj);

        NetworkCard netCard = cardObj.GetComponent<NetworkCard>();
        netCard.Initialize(pic, (uint)playerNumber);

        player.handCardNetIds.Add(netCard.netId);

        TargetAddCardToHand(player.connectionToClient, netCard.netId);
    }

    [TargetRpc]
    private void TargetAddCardToHand(NetworkConnection conn, uint cardNetId)
    {
        if (NetworkClient.spawned.TryGetValue(cardNetId, out NetworkIdentity identity))
        {
            Card card = identity.GetComponent<Card>();
            PlayingCardsTable.Instance.ReturnCardToHand(card);
        }
    }

    [Server]
    private void DrawIdeasCard()
    {
        currentIdeasCard = IdeasDeck.Instance.DrawCard();
        if (currentIdeasCard == null) CheckGameEndConditions();

        secretWordIndex = currentIdeasCard.GetRandomWord();

        foreach (var kvp in players)
        {
            NetworkPlayer player = kvp.Value;

            if (kvp.Key == currentPlayerNumber)
            {
                TargetShowActiveRoleView(player.connectionToClient, currentIdeasCard, secretWordIndex);
            }
            else
            {
                TargetShowOthersView(player.connectionToClient, currentIdeasCard);
            }
        }
    }

    [TargetRpc]
    private void TargetShowActiveRoleView(NetworkConnection conn, IdeasCard card, int wordIndex)
    {
        IdeasCardUI.Instance.ShowForActiveRole(card, wordIndex);
    }

    [TargetRpc]
    private void TargetShowOthersView(NetworkConnection conn, IdeasCard card)
    {
        IdeasCardUI.Instance.ShowForOthers(card);
    }

    [Server]
    public void OnPlayerFinishedTurn(int playerNumber)
    {
        if (playerNumber != currentPlayerNumber) return;

        currentPhase = GamePhase.Discussion;
        RpcStartDiscussion();

        Invoke(nameof(EndDiscussion), discussionTime);
    }

    [ClientRpc]
    private void RpcStartDiscussion()
    {
        NetworkChat.Instance.EnableDiscussionMode();
        TimerUI.Instance.StartTimer(30f, null);
    }

    [Server]
    private void EndDiscussion()
    {
        int activeIndex = playersOrder.IndexOf(currentPlayerNumber);
        int decisiveIndex = (activeIndex - 1 + playersOrder.Count) % playersOrder.Count;
        int decisivePlayerNumber = playersOrder[decisiveIndex];

        RpcShowGuessPanel(decisivePlayerNumber);
    }

    [ClientRpc]
    private void RpcShowGuessPanel(int decisivePlayerNumber)
    {
        if (players.TryGetValue(decisivePlayerNumber, out NetworkPlayer player) && player.isLocalPlayer)
        {
            IdeasCardUI.Instance.ShowGuessPanel(currentIdeasCard);
        }
        else
        {
            NetworkChat.Instance.AddSystemMessage($"Игрок {decisivePlayerNumber} угадывает слово...");
        }
    }

    [Server]
    public void OnWordGuessed(int playerNumber, int guessedWordIndex)
    {
        int activeIndex = playersOrder.IndexOf(currentPlayerNumber);
        int decisiveIndex = (activeIndex - 1 + playersOrder.Count) % playersOrder.Count;
        int decisivePlayerNumber = playersOrder[decisiveIndex];

        if (playerNumber != decisivePlayerNumber) return;

        bool isCorrect = (guessedWordIndex == secretWordIndex);

        if (isCorrect)
        {
            personalitiesScore++;
            RpcShowMessage($"Игрок {playerNumber} угадал! Очко личностям.");
        }
        else
        {
            dannyScore++;
            RpcShowMessage($"Игрок {playerNumber} не угадал! Очко Дэнни.");
        }

        CheckGameEndConditions();

        if (currentPhase != GamePhase.GameEnd && currentPhase != GamePhase.FinalRound)
        {
            StartNextTurn();
        }
    }

    [ClientRpc]
    private void RpcShowMessage(string message)
    {
        NetworkChat.Instance.AddSystemMessage(message);
    }

    [Server]
    private void CheckGameEndConditions()
    {
        if (personalitiesScore >= 6)
        {
            EndGame(false);
        }
        else if (dannyScore >= 3 || !PicturesDeck.Instance.EnoughCardsToDraw())
        {
            currentPhase = GamePhase.FinalRound;
            RpcStartFinalRound();
        }
    }

    [ClientRpc]
    private void RpcStartFinalRound()
    {
        FinalRoundManager.Instance.StartFinalRound();
    }

    [Server]
    public void EndGame(bool dannyWins)
    {
        currentPhase = GamePhase.GameEnd;
        RpcGameEnded(dannyWins);
    }

    [ClientRpc]
    private void RpcGameEnded(bool dannyWins)
    {
        LobbyManager.Instance.ShowGameEndScreen(dannyWins);
    }

    [Server]
    public void ProcessVote(int voterNumber, int suspectedNumber)
    {
        //FinalRoundManager.Instance.OnVoteReceived(voterNumber, suspectedNumber);
    }

    [Server]
    public void ReturnToLobby()
    {
        currentPhase = GamePhase.Lobby;
        personalitiesScore = 0;
        dannyScore = 0;
        currentPlayerNumber = 0;
        dannyPlayerNumber = 0;
        playersOrder.Clear();
        
        RpcReturnToLobby();
    }

    [ClientRpc]
    private void RpcReturnToLobby()
    {
        PlayingCardsTable.Instance.ClearTable();
        PlayingCardsTable.Instance.ClearHand();
        ScoreUI.Instance.ResetScore();
        LobbyManager.Instance.ShowLobby();
    }

    private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        Debug.Log($"Фаза изменена: {oldPhase} -> {newPhase}");
    }
    
    [Server]
    public bool IsHost(int playerNumber)
    {
        return playerNumber == hostPlayerNumber;
    }
}