using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Центральный игровой менеджер. Управляет комнатами и всей игровой логикой внутри каждой комнаты.
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameRoom gameRoomPrefab;
    [SerializeField] private GamePlayer gamePlayerPrefab;
    [SerializeField] private GameObject networkCardPrefab;

    [Header("Game Settings")]
    [SerializeField] private float turnTime = 90f;
    [SerializeField] private float discussionTime = 60f;

    private readonly Dictionary<string, RoomGameState> _rooms = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    #region Управление комнатами (Server)

    [Server]
    public void ServerCreateRoom(NetworkPlayer player)
    {
        if (player == null || !string.IsNullOrEmpty(player.CurrentRoomCode)) return;

        string code = GenerateCode();
        GameRoom room = Instantiate(gameRoomPrefab);
        room.RoomCode = code;
        NetworkServer.Spawn(room.gameObject);

        _rooms[code] = new RoomGameState(room);

        room.TryAddPlayer(player);
        player.CurrentRoomCode = code;
        player.IsHost = true;
        player.TargetRoomCreated(player.connectionToClient, code);
        Debug.Log($"[Server] Room {code} created by conn {player.connectionToClient?.connectionId}");
    }

    [Server]
    public void ServerJoinRoom(string code, NetworkPlayer player)
    {
        if (player == null) return;

        if (!_rooms.TryGetValue(code, out var state))
        {
            player.TargetRoomError(player.connectionToClient, "Комната не найдена");
            return;
        }
        if (!state.Room.TryAddPlayer(player))
        {
            player.TargetRoomError(player.connectionToClient, "Комната заполнена или игра уже идёт");
            return;
        }
        player.CurrentRoomCode = code;
        player.TargetJoinedRoom(player.connectionToClient, code);
        RpcRoomUpdated(code);
    }

    [Server]
    public void ServerRequestStartGame(NetworkPlayer player)
    {
        if (player == null || !player.IsHost) return;
        if (!_rooms.TryGetValue(player.CurrentRoomCode, out var state) || !state.Room.CanStart) return;
        ServerStartGame(state);
    }

    [Server]
    public void ServerRequestReturnToLobby(NetworkPlayer player)
    {
        if (player == null || string.IsNullOrEmpty(player.CurrentRoomCode)) return;
        ServerReturnToLobby(player.CurrentRoomCode);
    }

    [Server]
    public void ServerSetRoomPrivacy(NetworkPlayer player, bool isPrivate)
    {
        if (player == null || !player.IsHost) return;
        if (_rooms.TryGetValue(player.CurrentRoomCode, out var state))
            state.Room.IsPrivate = isPrivate;
    }

    #endregion

    #region Управление игроками (Server)

    [Server]
    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        if (!string.IsNullOrEmpty(player.CurrentRoomCode))
            ServerLeaveRoom(player);
    }

    [Server]
    public void ServerLeaveRoom(NetworkPlayer player)
    {
        string code = player.CurrentRoomCode;
        if (string.IsNullOrEmpty(code) || !_rooms.TryGetValue(code, out var state)) return;

        state.Room.RemovePlayer(player);
        player.CurrentRoomCode = string.Empty;
        player.IsHost = false;

        if (state.Room.PlayerCount == 0)
        {
            _rooms.Remove(code);
            NetworkServer.Destroy(state.Room.gameObject);
        }
        else
        {
            RpcRoomUpdated(code);
        }
    }

    #endregion

    #region Старт игры

    [Server]
    private void ServerStartGame(RoomGameState state)
    {
        state.Room.IsInProgress = true;
        var players = new List<NetworkPlayer>(state.Room.Players);
        int dannyIdx = Random.Range(0, players.Count);

        state.DanyIndex = dannyIdx;
        state.DanyLobbyNumber = players[dannyIdx].Number;

        for (int i = 0; i < players.Count; i++)
        {
            GamePlayer gp = Instantiate(gamePlayerPrefab);
            gp.RoomIndex   = i;
            gp.LobbyNumber = players[i].Number;
            gp.IsDanny     = (i == dannyIdx);
            gp.OwnerNetId  = players[i].netId;
            NetworkServer.Spawn(gp.gameObject, players[i].connectionToClient);
            players[i].GamePlayerNetId = gp.netId;
            state.GamePlayers.Add(gp);
        }

        state.CurrentIndex = state.GamePlayers.Count - 1;

        RpcGameStarted(state.Room.RoomCode);
        StartCoroutine(DelayedAction(0.5f, state.Room.RoomCode,
            () => ServerDistributeRoles(state.Room.RoomCode)));
    }

    [Server]
    private void ServerDistributeRoles(string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        state.Room.Phase = GamePhase.RoleDistribution;

        foreach (var gp in state.GamePlayers)
            gp.TargetSendRole(gp.connectionToClient, gp.IsDanny);

        StartCoroutine(DelayedAction(3f, roomCode, () => ServerStartNextTurn(roomCode)));
    }

    #endregion

    #region Ходы

    [Server]
    public void ServerStartNextTurn(string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        state.Room.Phase = GamePhase.TurnInProgress;

        state.DecisiveIndex = state.CurrentIndex;

        int count = state.GamePlayers.Count;
        state.CurrentIndex = (state.CurrentIndex + 1) % count;

        foreach (var gp in state.GamePlayers)
        {
            gp.HasFinishedTurn = false;
            if (gp.RoomIndex == state.CurrentIndex) gp.Role = Role.Active;
            else if (gp.RoomIndex == state.DecisiveIndex)  gp.Role = Role.Decisive;
            else gp.Role = Role.Waiting;
        }

        // Убрать потом
        //PlayerListGameUI.Instance.RefreshList();
        ServerDrawCardsForPlayers(state);
        ServerDrawIdeasCard(state);
        RpcStartTurn(roomCode);
    }

    [ClientRpc]
    private void RpcStartTurn(string roomCode)
    {
        TimerUI.Instance.StartTimer(turnTime, () => OnTurnTimerEnded(roomCode));
    }

    private void OnTurnTimerEnded(string roomCode)
    {
        if (NetworkClient.localPlayer != null)
        {
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().CmdTurnTimerEnded(roomCode);
        }
    }

    [Server]
    private void ServerDrawCardsForPlayers(RoomGameState state)
    {
        foreach (var gp in state.GamePlayers)
        {
            if (gp.Role != Role.Active) continue;
            gp.HandCardNetIds.Clear();
            for (int i = 0; i < 7; i++)
            {
                Sprite pic = PicturesDeck.Instance.DrawCard();
                if (pic == null) { ServerCheckGameEnd(state); return; }

                int spriteIdx = CardsStorage.PictureCardsSprites.IndexOf(pic);

                GameObject cardObj = Instantiate(networkCardPrefab);
                NetworkCard netCard = cardObj.GetComponent<NetworkCard>();
                if (netCard == null)
                {
                    Debug.LogError($"[Server] networkCardPrefab '{networkCardPrefab?.name}' не содержит NetworkCard!");
                    Destroy(cardObj);
                    return;
                }
                netCard.Initialize(spriteIdx, gp.OwnerNetId);
                NetworkServer.Spawn(cardObj, gp.connectionToClient);

                gp.HandCardNetIds.Add(netCard.netId);
                bool canInteract = gp.Role == Role.Active;
                gp.TargetAddCardToHand(gp.connectionToClient, netCard.netId, canInteract);
            }
        }
    }

    [Server]
    private void ServerDrawIdeasCard(RoomGameState state)
    {
        state.CurrentIdeasCard = IdeasDeck.Instance.DrawCard();
        if (state.CurrentIdeasCard == null) { ServerCheckGameEnd(state); return; }
        state.SecretWordIndex = state.CurrentIdeasCard.GetRandomWord();

        foreach (var gp in state.GamePlayers)
        {
            if (gp.Role == Role.Active)
                gp.TargetShowActiveView(gp.connectionToClient, state.CurrentIdeasCard, state.SecretWordIndex);
            else
                gp.TargetShowOthersView(gp.connectionToClient, state.CurrentIdeasCard);
        }
    }

    #endregion

    #region Игровые события

    [Server]
    public void ServerOnPlayerFinishedTurn(GamePlayer gp)
    {
        string roomCode = FindRoomCode(gp);
        if (roomCode == null || !_rooms.TryGetValue(roomCode, out var state)) return;
        if (gp.RoomIndex != state.CurrentIndex) return;

        state.Room.Phase = GamePhase.Discussion;

        ServerToggleCardsInteraction(state, false);

        RpcStartDiscussion(roomCode);
        ServerEnableWordsForDecisive(state);

        StartCoroutine(DelayedAction(discussionTime, roomCode, () =>
        {
            if (_rooms.TryGetValue(roomCode, out var s) && s.Room.Phase == GamePhase.Discussion)
                ServerEndDiscussion(s);
        }));
    }

    [Server]
    public void ServerOnTurnTimerEnded(NetworkPlayer player, string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        if (state.Room.Phase != GamePhase.TurnInProgress) return;
        if (player == null || !player.IsHost) return;
        GamePlayer gp = state.GamePlayers.Find(p => p.RoomIndex == state.CurrentIndex);
        if (gp != null) ServerOnPlayerFinishedTurn(gp);
    }

    [Server]
    private void ServerEnableWordsForDecisive(RoomGameState state)
    {
        GamePlayer decisiveGp = state.GamePlayers.Find(p => p.RoomIndex == state.DecisiveIndex);
        if (decisiveGp == null) return;
        decisiveGp.TargetShowGuessPanel(decisiveGp.connectionToClient, state.CurrentIdeasCard);
    }

    [Server]
    private void ServerToggleCardsInteraction(RoomGameState state, bool interactable)
    {
        GamePlayer activePlayer = state.GamePlayers.Find(p => p.RoomIndex == state.CurrentIndex);
        if (activePlayer == null) return;

        if (!interactable)
        {
            foreach (uint cardNetId in activePlayer.HandCardNetIds)
            {
                if (NetworkClient.spawned.TryGetValue(cardNetId, out NetworkIdentity identity))
                {
                    NetworkServer.Destroy(identity.gameObject);
                }
            }
            activePlayer.HandCardNetIds.Clear();
        }

        NetworkCard[] cardsOnTable = PlayingCardsTable.Instance.GetComponentsInChildren<NetworkCard>();
        foreach (NetworkCard card in cardsOnTable)
        {
            card.CmdToggleCardInteraction(interactable);
        }
    }

    [Server]
    private void ServerEndDiscussion(RoomGameState state)
    {
        GamePlayer decisiveGp = state.GamePlayers.Find(p => p.RoomIndex == state.DecisiveIndex);
        if (decisiveGp == null) return;
        decisiveGp.TargetShowGuessPanel(decisiveGp.connectionToClient, state.CurrentIdeasCard);
    }

    [Server]
    public void ServerOnWordGuessed(GamePlayer gp, int wordIndex)
    {
        string roomCode = FindRoomCode(gp);
        if (roomCode == null || !_rooms.TryGetValue(roomCode, out var state)) return;
        if (gp.RoomIndex != state.DecisiveIndex) return;

        if (wordIndex == state.SecretWordIndex)
        {
            state.Room.PersonalitiesScore++;
            RpcShowMessage(roomCode, "Правильно! Личности получают очко.");
        }
        else
        {
            state.Room.DannyScore++;
            RpcShowMessage(roomCode, "Неправильно! Дэнни получает очко.");
        }

        if (!ServerCheckGameEnd(state))
            ServerStartNextTurn(roomCode);
    }

    [Server]
    public void ServerOnVoteReceived(NetworkPlayer voter, int suspectedLobbyNumber)
    {
        if (string.IsNullOrEmpty(voter.CurrentRoomCode)) return;
        if (!_rooms.TryGetValue(voter.CurrentRoomCode, out var state)) return;
        state.Votes[voter.Number] = suspectedLobbyNumber;

        if (state.Votes.Count >= state.GamePlayers.Count)
            ServerResolveVotes(state);
    }

    [Server]
    private void ServerResolveVotes(RoomGameState state)
    {
        var tally = new Dictionary<int, int>();
        foreach (int suspect in state.Votes.Values)
        {
            if (!tally.ContainsKey(suspect)) tally[suspect] = 0;
            tally[suspect]++;
        }

        int maxVotes = 0;
        foreach (int cnt in tally.Values) if (cnt > maxVotes) maxVotes = cnt;

        var topVoted = new List<int>();
        foreach (var kvp in tally) if (kvp.Value == maxVotes) topVoted.Add(kvp.Key);

        if (topVoted.Count == 1)
        {
            bool dannyFound = topVoted[0] == state.DanyLobbyNumber;
            NetworkFinalRoundManager.Instance.RpcShowVoteResult(topVoted[0], dannyFound);
            ServerEndGame(state, dannyWins: !dannyFound);
        }
        else
        {
            NetworkFinalRoundManager.Instance.RpcHandleTie(state.DanyLobbyNumber, topVoted);
            state.Votes.Clear();
        }
    }

    [Server]
    public void ServerEndGame(RoomGameState state, bool dannyWins)
    {
        state.Room.Phase = GamePhase.GameEnd;
        RpcGameEnded(state.Room.RoomCode, dannyWins);
    }

    [Server]
    public void ServerEndGameByVote(string roomCode, bool dannyWins)
    {
        if (_rooms.TryGetValue(roomCode, out var state))
            ServerEndGame(state, dannyWins);
    }

    [Server]
    private bool ServerCheckGameEnd(RoomGameState state)
    {
        if (state.Room.PersonalitiesScore >= 6)
        {
            state.Room.Phase = GamePhase.GameEnd;
            RpcGameEnded(state.Room.RoomCode, false);
            return true;
        }
        if (state.Room.DannyScore >= 3 || !PicturesDeck.Instance.EnoughCardsToDraw())
        {
            state.Room.Phase = GamePhase.FinalRound;
            RpcStartFinalRound(state.Room.RoomCode, state.DanyLobbyNumber);
            return true;
        }
        return false;
    }

    [Server]
    private void ServerReturnToLobby(string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;

        foreach (var gp in state.GamePlayers)
            if (gp != null) NetworkServer.Destroy(gp.gameObject);

        state.GamePlayers.Clear();
        state.Votes.Clear();
        state.CurrentIdeasCard = null;
        state.CurrentIndex  = -1;
        state.DecisiveIndex = -1;

        state.Room.IsInProgress      = false;
        state.Room.Phase             = GamePhase.Lobby;
        state.Room.PersonalitiesScore = 0;
        state.Room.DannyScore        = 0;

        RpcReturnToLobby(roomCode);
    }

    #endregion

    #region Вспомогательные методы

    public List<int> GetPlayersNumbers(string roomCode)
    {
        if (_rooms.TryGetValue(roomCode, out var state))
            return state.GamePlayers.ConvertAll(gp => gp.LobbyNumber);
        return new List<int>();
    }

    public RoomGameState GetState(string roomCode)
    {
        _rooms.TryGetValue(roomCode, out var state);
        return state;
    }

    [Server]
    private string FindRoomCode(GamePlayer gp)
    {
        if (NetworkServer.spawned.TryGetValue(gp.OwnerNetId, out NetworkIdentity id))
            return id.GetComponent<NetworkPlayer>().CurrentRoomCode;
        return null;
    }

    [Server]
    private string GenerateCode()
    {
        string code;
        do { code = Random.Range(10000, 99999).ToString(); }
        while (_rooms.ContainsKey(code));
        return code;
    }

    private IEnumerator DelayedAction(float delay, string roomCode, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        if (_rooms.ContainsKey(roomCode))
            action?.Invoke();
    }

    #endregion

    #region События для всех клиентов

    [ClientRpc]
    private void RpcRoomUpdated(string code)
        => LobbyManager.Instance?.RefreshRoomPanel();

    [ClientRpc]
    private void RpcGameStarted(string code)
        => LobbyManager.Instance?.OnGameStarted();

    [ClientRpc]
    private void RpcStartDiscussion(string code)
    {
        NetworkChat.Instance?.EnableDiscussionMode();
        TimerUI.Instance?.StartTimer(60f, null);
    }

    [ClientRpc]
    private void RpcShowMessage(string code, string msg)
        => NetworkChat.Instance?.AddSystemMessage(msg);

    [ClientRpc]
    private void RpcStartFinalRound(string code, int dannyLobbyNumber)
        => NetworkFinalRoundManager.Instance?.StartFinalRound(dannyLobbyNumber, code);

    [ClientRpc]
    private void RpcGameEnded(string code, bool dannyWins)
        => LobbyManager.Instance?.ShowGameEndScreen(dannyWins);

    [ClientRpc]
    private void RpcReturnToLobby(string code)
    {
        PlayingCardsTable.Instance?.ClearTable();
        PlayingCardsTable.Instance?.ClearHand();
        ScoreUI.Instance?.ResetScore();
        LobbyManager.Instance?.ShowLobby();
    }

    #endregion
}

public class RoomGameState
{
    public GameRoom Room;
    public int CurrentIndex  = -1;
    public int DecisiveIndex = -1;
    public int DanyIndex;
    public int DanyLobbyNumber;
    public List<GamePlayer>    GamePlayers      = new();
    public IdeasCard           CurrentIdeasCard;
    public int                 SecretWordIndex;
    public Dictionary<int,int> Votes            = new(); // (voterLobbyNumber, suspectedLobbyNumber)

    public RoomGameState(GameRoom room) => Room = room;
}
