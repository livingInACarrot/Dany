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
    [SerializeField] private float discussionTime = 40f;

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
            player.TargetRoomError(player.connectionToClient, Loc.Text("error.roomNotFound"));
            return;
        }
        if (!state.Room.TryAddPlayer(player))
        {
            player.TargetRoomError(player.connectionToClient, Loc.Text("error.roomFull"));
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
        if (!_rooms.TryGetValue(player.CurrentRoomCode, out var state)) return;
        ServerReturnToLobby(state);
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

        int playerNumber = player.Number;
        state.Room.RemovePlayer(player);
        player.CurrentRoomCode = string.Empty;
        player.IsHost = false;

        if (state.Room.PlayerCount == 0)
        {
            _rooms.Remove(code);
            NetworkServer.Destroy(state.Room.gameObject);
            return;
        }

        RpcShowMessage(code, $"{Loc.Nick(playerNumber)} {Loc.Text("chat.playerLeft")}");

        if (state.Room.IsInProgress && state.Room.PlayerCount < GameRoom.MinPlayers)
        {
            string msg = $"{Loc.Nick(playerNumber)} {Loc.Text("abortionText")}\n" +
                $"{Loc.Text("gameUI.score.personalities")}: {state.Room.PersonalitiesScore} | {Loc.Text("gameUI.score.dany")}: {state.Room.DanyScore}";
            RpcShowAbortionPanel(code, msg);
            return;
        }
        RpcRoomUpdated(code);
    }

    #endregion

    #region Старт игры

    [Server]
    private void ServerStartGame(RoomGameState state)
    {
        state.Room.IsInProgress = true;
        var players = new List<NetworkPlayer>(state.Room.Players);
        int danyIdx = Random.Range(0, players.Count);

        state.DanyIndex = danyIdx;
        state.DanyLobbyNumber = players[danyIdx].Number;

        for (int i = 0; i < players.Count; i++)
        {
            GamePlayer gp = Instantiate(gamePlayerPrefab);
            gp.RoomIndex   = i;
            gp.LobbyNumber = players[i].Number;
            gp.IsDany      = (i == danyIdx);
            gp.OwnerNetId  = players[i].netId;
            NetworkServer.Spawn(gp.gameObject, players[i].connectionToClient);
            players[i].GamePlayerNetId = gp.netId;
            state.GamePlayers.Add(gp);
        }

        state.CurrentIndex = state.GamePlayers.Count - 1;

        RpcGameStarted(state.Room.RoomCode);
        StartCoroutine(DelayedAction(0.5f, state, () => ServerDistributeRoles(state)));
    }

    [Server]
    private void ServerDistributeRoles(RoomGameState state)
    {
        state.Room.Phase = GamePhase.RoleDistribution;

        foreach (var gp in state.GamePlayers)
            gp.TargetSendRole(gp.connectionToClient, gp.IsDany);

        StartCoroutine(DelayedAction(3f, state, () => ServerStartNextTurn(state)));
    }

    #endregion

    #region Ходы

    [Server]
    private void ServerStartNextTurn(RoomGameState state)
    {
        ServerDestroyActivePlayerCards(state);

        state.Room.Phase = GamePhase.TurnInProgress;
        state.DecisiveIndex = state.CurrentIndex;
        state.CurrentIndex = (state.CurrentIndex + 1) % state.GamePlayers.Count;

        foreach (var gp in state.GamePlayers)
        {
            gp.HasFinishedTurn = false;
            if (gp.RoomIndex == state.CurrentIndex)       gp.Role = Role.Active;
            else if (gp.RoomIndex == state.DecisiveIndex) gp.Role = Role.Decisive;
            else                                           gp.Role = Role.Waiting;
        }

        ServerDrawCardsForPlayers(state);
        ServerDrawIdeasCard(state);
        RpcStartTurn(state.Room.RoomCode);
    }

    [Server]
    private void ServerDestroyActivePlayerCards(RoomGameState state)
    {
        GamePlayer prevActive = state.GamePlayers.Find(p => p.RoomIndex == state.CurrentIndex);
        if (prevActive == null) return;
        foreach (uint cardNetId in prevActive.HandCardNetIds)
        {
            if (NetworkServer.spawned.TryGetValue(cardNetId, out NetworkIdentity identity))
                NetworkServer.Destroy(identity.gameObject);
        }
        prevActive.HandCardNetIds.Clear();
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
                netCard.Initialize(spriteIdx, gp.OwnerNetId);
                NetworkServer.Spawn(cardObj, gp.connectionToClient);

                gp.HandCardNetIds.Add(netCard.netId);
                gp.TargetAddCardToHand(gp.connectionToClient, netCard.netId, canInteract: true);
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
        if (!TryFindState(gp, out var state)) return;
        if (gp.RoomIndex != state.CurrentIndex) return;

        state.Room.Phase = GamePhase.Discussion;

        gp.TargetClearHand(gp.connectionToClient);
        RpcToggleCardsInteraction(false);

        ServerEnableWordsForDecisive(state);
        RpcStartDiscussion(state.Room.RoomCode);
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
    public void ServerOnDecisiveTimerEnded(NetworkPlayer player, string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        if (state.Room.Phase != GamePhase.Discussion) return;
        if (player == null || !player.IsHost) return;

        state.Room.DanyScore++;
        RpcShowLocalizedMessage(state.Room.RoomCode, "lostByTimeout");
        StartCoroutine(DelayedAction(3f, state, () => ServerStartNextTurn(state)));
    }

    [Server]
    private void ServerEnableWordsForDecisive(RoomGameState state)
    {
        GamePlayer decisiveGp = state.GamePlayers.Find(p => p.RoomIndex == state.DecisiveIndex);
        if (decisiveGp == null) return;
        decisiveGp.TargetShowGuessPanel(decisiveGp.connectionToClient, state.CurrentIdeasCard);
    }

    [Server]
    public void ServerOnWordGuessed(GamePlayer gp, int wordIndex)
    {
        if (!TryFindState(gp, out var state)) return;
        if (gp.RoomIndex != state.DecisiveIndex) return;

        if (wordIndex == state.SecretWordIndex)
        {
            state.Room.PersonalitiesScore++;
            RpcShowLocalizedMessage(state.Room.RoomCode, "chat.correctGuess");
        }
        else
        {
            state.Room.DanyScore++;
            RpcShowLocalizedMessage(state.Room.RoomCode, "chat.wrongGuess");
        }

        if (!ServerCheckGameEnd(state))
            ServerStartNextTurn(state);
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
    private void ServerEndGame(RoomGameState state, bool dannyWins)
    {
        state.Room.Phase = GamePhase.GameEnd;
        RpcGameEnded(state.Room.RoomCode, dannyWins);
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
        if (state.Room.DanyScore >= 3 || !PicturesDeck.Instance.EnoughCardsToDraw())
        {
            state.Room.Phase = GamePhase.FinalRound;
            var numbers = state.GamePlayers.ConvertAll(gp => gp.LobbyNumber);
            RpcStartFinalRound(state.Room.RoomCode, state.DanyLobbyNumber, numbers);
            return true;
        }
        return false;
    }

    [Server]
    private void ServerReturnToLobby(RoomGameState state)
    {
        foreach (var gp in state.GamePlayers)
            if (gp != null) NetworkServer.Destroy(gp.gameObject);

        state.GamePlayers.Clear();
        state.Votes.Clear();
        state.CurrentIdeasCard = null;
        state.CurrentIndex  = -1;
        state.DecisiveIndex = -1;

        state.Room.IsInProgress       = false;
        state.Room.Phase              = GamePhase.Lobby;
        state.Room.PersonalitiesScore = 0;
        state.Room.DanyScore          = 0;

        RpcReturnToLobby(state.Room.RoomCode);
    }

    #endregion

    #region Вспомогательные методы

    public RoomGameState GetState(string roomCode)
    {
        _rooms.TryGetValue(roomCode, out var state);
        return state;
    }

    [Server]
    private bool TryFindState(GamePlayer gp, out RoomGameState state)
    {
        state = null;
        if (!NetworkServer.spawned.TryGetValue(gp.OwnerNetId, out NetworkIdentity id)) return false;
        string roomCode = id.GetComponent<NetworkPlayer>()?.CurrentRoomCode;
        if (string.IsNullOrEmpty(roomCode)) return false;
        return _rooms.TryGetValue(roomCode, out state);
    }

    [Server]
    private string GenerateCode()
    {
        string code;
        do { code = Random.Range(10000, 99999).ToString(); }
        while (_rooms.ContainsKey(code));
        return code;
    }

    private IEnumerator DelayedAction(float delay, RoomGameState state, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        if (_rooms.ContainsKey(state.Room.RoomCode))
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
    private void RpcStartTurn(string roomCode)
    {
        TimerUI.Instance.StartTimer(turnTime, () => OnTurnTimerEnded(roomCode));
    }

    [ClientRpc]
    private void RpcStartDiscussion(string roomCode)
    {
        TimerUI.Instance.StartTimer(discussionTime, () => OnDiscussionTimerEnded(roomCode));
    }

    [ClientRpc]
    private void RpcToggleCardsInteraction(bool active)
    {
        PlayingCardsTable.Instance.ToggleInteractions(active);
    }

    [ClientRpc]
    private void RpcShowMessage(string code, string msg)
        => NetworkChat.Instance.AddSystemMessage(msg);

    [ClientRpc]
    private void RpcShowLocalizedMessage(string code, string locKey)
        => NetworkChat.Instance.AddSystemMessage(Loc.Text(locKey));

    [ClientRpc]
    private void RpcStartFinalRound(string code, int danyLobbyNumber, List<int> lobbyNumbers)
        => NetworkFinalRoundManager.Instance.StartFinalRound(danyLobbyNumber, code, lobbyNumbers);

    [ClientRpc]
    private void RpcGameEnded(string code, bool danyWins)
        => LobbyManager.Instance.ShowGameEndScreen(danyWins);

    [ClientRpc]
    private void RpcShowAbortionPanel(string code, string message)
        => LobbyManager.Instance.ShowAbortionScreen(message);

    [ClientRpc]
    private void RpcReturnToLobby(string code)
    {
        PlayingCardsTable.Instance.ClearTable();
        PlayingCardsTable.Instance.ClearHand();
        ScoreUI.Instance.ResetScore();
        LobbyManager.Instance.ShowLobby();
    }

    #endregion

    #region Таймеры (Client)

    private void OnTurnTimerEnded(string roomCode)
    {
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>().CmdTurnTimerEnded(roomCode);
    }

    private void OnDiscussionTimerEnded(string roomCode)
    {
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>().CmdDecisiveTimerEnded(roomCode);
    }

    #endregion
}

public class RoomGameState
{
    public GameRoom Room;
    public int CurrentIndex = -1;
    public int DecisiveIndex = -1;
    public int DanyIndex;
    public int DanyLobbyNumber;
    public List<GamePlayer> GamePlayers = new();
    public IdeasCard CurrentIdeasCard;
    public int SecretWordIndex;
    public Dictionary<int, int> Votes = new();

    public RoomGameState(GameRoom room) => Room = room;
}
