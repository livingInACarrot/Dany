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

    [Header("Game Settings")]
    [SerializeField] private float discussionTime = 60f;

    // СЕРВЕР
    private readonly Dictionary<string, GameRoom> _rooms = new Dictionary<string, GameRoom>();
    private readonly Dictionary<string, RoomGameState> _states = new Dictionary<string, RoomGameState>();

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
        _rooms[code] = room;

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

        if (!_rooms.TryGetValue(code, out GameRoom room))
        {
            player.TargetRoomError(player.connectionToClient, "Комната не найдена");
            return;
        }
        if (!room.TryAddPlayer(player))
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
        if (!_rooms.TryGetValue(player.CurrentRoomCode, out GameRoom room) || !room.CanStart) return;
        ServerStartGame(room);
    }

    [Server]
    public void ServerRequestReturnToLobby(NetworkPlayer player)
    {
        if (player == null || !player.IsHost) return;
        ServerReturnToLobby(player.CurrentRoomCode);
    }
    #endregion

    #region Управление комнатами (Server)
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
        if (string.IsNullOrEmpty(code) || !_rooms.TryGetValue(code, out GameRoom room)) return;

        room.RemovePlayer(player);
        player.CurrentRoomCode = string.Empty;
        player.IsHost = false;

        if (room.PlayerCount == 0)
        {
            _rooms.Remove(code);
            _states.Remove(code);
            NetworkServer.Destroy(room.gameObject);
        }
        else
        {
            RpcRoomUpdated(code);
        }
    }
    #endregion

    #region Старт игры
    [Server]
    private void ServerStartGame(GameRoom room)
    {
        room.IsInProgress = true;
        var players = new List<NetworkPlayer>(room.Players);
        int dannyIdx = Random.Range(0, players.Count);

        var state = new RoomGameState(room);
        _states[room.RoomCode] = state;
        state.DanyIndex = dannyIdx;

        for (int i = 0; i < players.Count; i++)
        {
            GamePlayer gp = Instantiate(gamePlayerPrefab);
            gp.RoomIndex = i;
            gp.IsDanny = (i == dannyIdx);
            gp.OwnerNetId = players[i].netId;
            NetworkServer.Spawn(gp.gameObject, players[i].connectionToClient);
            players[i].GamePlayerNetId = gp.netId;
            state.GamePlayers.Add(gp);
        }

        RpcGameStarted(room.RoomCode);
        StartCoroutine(DelayedAction(0.5f, room.RoomCode, () => ServerDistributeRoles(room.RoomCode)));
    }

    [Server]
    private void ServerDistributeRoles(string roomCode)
    {
        if (!_states.TryGetValue(roomCode, out var state)) return;
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
        if (!_states.TryGetValue(roomCode, out var state)) return;
        state.Room.Phase = GamePhase.TurnInProgress;

        // Решающий — тот, кто был активным в прошлом ходу
        state.DecisiveIndex = state.CurrentIndex;

        // Следующий активный — просто следующий по кругу
        int count = state.GamePlayers.Count;
        state.CurrentIndex = (state.CurrentIndex + 1) % count;

        foreach (var gp in state.GamePlayers)
        {
            gp.HasFinishedTurn = false;
            if (gp.RoomIndex == state.CurrentIndex)       gp.Role = Role.Active;
            else if (gp.RoomIndex == state.DecisiveIndex) gp.Role = Role.Decisive;
            else                                           gp.Role = Role.Waiting;
        }

        ServerDrawCardsForPlayers(state);
        ServerDrawIdeasCard(state);
    }

    [Server]
    private void ServerDrawCardsForPlayers(RoomGameState state)
    {
        foreach (var gp in state.GamePlayers)
        {
            if (gp.Role == Role.Waiting) continue;
            gp.HandCardNetIds.Clear();
            for (int i = 0; i < 7; i++)
            {
                Sprite pic = PicturesDeck.Instance.DrawCard();
                if (pic == null) { ServerCheckGameEnd(state); return; }

                GameObject cardObj = PlayingCardsTable.Instance.SpawnCardInHand();
                NetworkServer.Spawn(cardObj);
                NetworkCard netCard = cardObj.GetComponent<NetworkCard>();
                netCard.Initialize(pic, gp.netId);
                gp.HandCardNetIds.Add(netCard.netId);
                gp.TargetAddCardToHand(gp.connectionToClient, netCard.netId);
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
        if (roomCode == null || !_states.TryGetValue(roomCode, out var state)) return;
        if (gp.RoomIndex != state.CurrentIndex) return;

        state.Room.Phase = GamePhase.Discussion;
        RpcStartDiscussion(roomCode);
        StartCoroutine(DelayedAction(discussionTime, roomCode, () =>
        {
            if (_states.TryGetValue(roomCode, out var s) && s.Room.Phase == GamePhase.Discussion)
                ServerEndDiscussion(s);
        }));
    }

    [Server]
    private void ServerEndDiscussion(RoomGameState state)
    {
        GamePlayer decisiveGp = state.GamePlayers.Find(p => p.RoomIndex == state.DecisiveIndex);
        decisiveGp?.TargetShowGuessPanel(decisiveGp.connectionToClient, state.CurrentIdeasCard);
    }

    [Server]
    public void ServerOnWordGuessed(GamePlayer gp, int wordIndex)
    {
        string roomCode = FindRoomCode(gp);
        if (roomCode == null || !_states.TryGetValue(roomCode, out var state)) return;
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
    public void ServerOnVoteReceived(NetworkPlayer voter, int suspectedIndex)
    {
        if (string.IsNullOrEmpty(voter.CurrentRoomCode)) return;
        if (!_states.TryGetValue(voter.CurrentRoomCode, out var state)) return;
        state.Votes[voter.Number] = suspectedIndex;

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
            bool dannyFound = topVoted[0] == state.DanyIndex;
            NetworkFinalRoundManager.Instance.RpcShowVoteResult(topVoted[0], dannyFound);
            ServerEndGame(state, dannyWins: !dannyFound);
        }
        else
        {
            // Ничья — показываем второй раунд
            NetworkFinalRoundManager.Instance.RpcHandleTie(state.DanyIndex, topVoted);
            // Сбрасываем голоса для возможного второго раунда
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
        if (_states.TryGetValue(roomCode, out var state))
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
            RpcStartFinalRound(state.Room.RoomCode, state.DanyIndex);
            return true;
        }
        return false;
    }

    [Server]
    private void ServerReturnToLobby(string roomCode)
    {
        if (!_states.TryGetValue(roomCode, out var state)) return;
        foreach (var gp in state.GamePlayers)
            if (gp != null) NetworkServer.Destroy(gp.gameObject);
        _states.Remove(roomCode);

        if (_rooms.TryGetValue(roomCode, out var room))
        {
            room.IsInProgress = false;
            room.Phase = GamePhase.Lobby;
            room.PersonalitiesScore = 0;
            room.DannyScore = 0;
        }
        RpcReturnToLobby(roomCode);
    }
    #endregion

    #region Вспомогательные методы
    public List<int> GetPlayersNumbers(string roomCode)
    {
        if (_states.TryGetValue(roomCode, out var state))
            return state.GamePlayers.ConvertAll(gp => gp.RoomIndex);
        return new List<int>();
    }

    public RoomGameState GetState(string roomCode)
    {
        _states.TryGetValue(roomCode, out var state);
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
        if (_rooms.ContainsKey(roomCode)) // комната ещё существует
            action?.Invoke();
    }
    #endregion

    #region События для всех клиентов

    [ClientRpc]
    private void RpcRoomUpdated(string code)
        => LobbyManager.Instance.RefreshRoomPanel();

    [ClientRpc]
    private void RpcGameStarted(string code)
        => LobbyManager.Instance.OnGameStarted();

    [ClientRpc]
    private void RpcStartDiscussion(string code)
    {
        NetworkChat.Instance.EnableDiscussionMode();
        TimerUI.Instance.StartTimer(60f, null);
    }

    [ClientRpc]
    private void RpcShowMessage(string code, string msg)
        => NetworkChat.Instance.AddSystemMessage(msg);

    [ClientRpc]
    private void RpcStartFinalRound(string code, int dannyIndex)
        => NetworkFinalRoundManager.Instance.StartFinalRound(dannyIndex, code);

    [ClientRpc]
    private void RpcGameEnded(string code, bool dannyWins)
        => LobbyManager.Instance.ShowGameEndScreen(dannyWins);

    [ClientRpc]
    private void RpcReturnToLobby(string code)
    {
        PlayingCardsTable.Instance.ClearTable();
        PlayingCardsTable.Instance.ClearHand();
        ScoreUI.Instance.ResetScore();
        LobbyManager.Instance.ShowLobby();
    }
    #endregion
}

public class RoomGameState
{
    public GameRoom Room;
    public int CurrentIndex = -1;
    public int DecisiveIndex = -1;
    public int DanyIndex;
    public List<GamePlayer> GamePlayers = new();
    public IdeasCard CurrentIdeasCard;
    public int SecretWordIndex;
    public Dictionary<int, int> Votes = new(); // (voterNumber, suspectedIndex)

    public RoomGameState(GameRoom room) => Room = room;
}
