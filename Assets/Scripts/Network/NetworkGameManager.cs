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
    [SerializeField] private float votingTime = 60f;

    // Grace period (seconds) allowed between server-recorded start time and host-reported end.
    // Accounts for network latency and frame-timing drift.
    private const float TimerGracePeriod = 2f;

    private readonly Dictionary<string, RoomGameState> _rooms = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    #region Управление комнатами

    [Server]
    public void ServerCreateRoom(NetworkPlayer player)
    {
        if (player == null || !string.IsNullOrEmpty(player.CurrentRoomCode)) return;

        string code = GenerateCode();
        GameRoom room = Instantiate(gameRoomPrefab);
        room.RoomCode = code;
        NetworkServer.Spawn(room.gameObject);

        _rooms[code] = new RoomGameState(room) { MatchId = System.Guid.NewGuid() };

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
        if (state.ChatSenderNums.Count > 0)
            player.TargetReceiveChatHistory(player.connectionToClient, state.ChatSenderNums, state.ChatTexts);
        foreach (NetworkPlayer np in state.Room.Players)
            TargetRoomUpdated(np.connectionToClient);
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

        if (state.Room.Phase == GamePhase.GameEnd)
        {
            TargetReturnToLobby(player.connectionToClient);
            state.PlayersReturnedToLobby.Add(player.Number);
            TryFinalizeGameEnd(state);
        }
        else
        {
            ServerReturnToLobby(state);
        }
    }

    private void TryFinalizeGameEnd(RoomGameState state)
    {
        foreach (var np in state.Room.Players)
            if (!state.PlayersReturnedToLobby.Contains(np.Number)) return;
        ServerFinalizeGameEnd(state);
    }

    [Server]
    private void ServerFinalizeGameEnd(RoomGameState state)
    {
        foreach (var gp in state.GamePlayers)
            if (gp != null) NetworkServer.Destroy(gp.gameObject);

        state.GamePlayers.Clear();
        state.Votes.Clear();
        state.VotingResolved = false;
        state.PlayersReturnedToLobby.Clear();
        state.CurrentIdeasCard = null;
        state.CurrentIndex = -1;
        state.DecisiveIndex = -1;

        state.Room.IsInProgress = false;
        state.Room.Phase = GamePhase.Lobby;
        state.Room.PersonalitiesScore = 0;
        state.Room.DanyScore = 0;
    }

    [Server]
    public void ServerSetRoomPrivacy(NetworkPlayer player, bool isPrivate)
    {
        if (player == null || !player.IsHost) return;
        if (_rooms.TryGetValue(player.CurrentRoomCode, out var state))
            state.Room.IsPrivate = isPrivate;
    }

    [Server]
    public void ServerKickPlayer(NetworkPlayer requester, int targetNumber)
    {
        if (requester == null || !requester.IsHost) return;
        if (!_rooms.TryGetValue(requester.CurrentRoomCode, out var state)) return;

        NetworkPlayer target = null;
        foreach (NetworkPlayer p in state.Room.Players)
            if (p.Number == targetNumber) { target = p; break; }
        if (target == null || target.IsHost) return;

        target.TargetKicked(target.connectionToClient);
        ServerLeaveRoom(target);
    }

    #endregion

    #region Управление игроками

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

        if (state.Room.Phase == GamePhase.GameEnd)
        {
            TryFinalizeGameEnd(state);
            return;
        }

        string abortionMsg;
        foreach (NetworkPlayer np in state.Room.Players) {
            np.TargetShowPopup(np.connectionToClient, $"{Loc.NickFor(np, playerNumber)} {Loc.TextFor(np, "chat.playerLeft")}");

            if (state.Room.IsInProgress && state.Room.PlayerCount < GameRoom.MinPlayers) {
                abortionMsg = $"{Loc.NickFor(np, playerNumber)} {Loc.TextFor(np, "abortionText")}\n" +
                    $"{Loc.TextFor(np, "gameUI.score.personalities")}: {state.Room.PersonalitiesScore} | " +
                    $"{Loc.TextFor(np, "gameUI.score.dany")}: {state.Room.DanyScore}";
                TargetShowAbortionPanel(np.connectionToClient, abortionMsg);
            }
            else
            {
                TargetRoomUpdated(np.connectionToClient);
            }
        }
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
            gp.OwnerNetId  = players[i].netId;
            NetworkServer.Spawn(gp.gameObject, players[i].connectionToClient);
            gp.GetComponent<NetworkMatch>().matchId = state.MatchId;
            players[i].GamePlayerNetId = gp.netId;
            state.GamePlayers.Add(gp);
        }

        state.CurrentIndex = Random.Range(0, state.GamePlayers.Count);

        foreach (NetworkPlayer np in state.Room.Players)
            TargetGameStarted(np.connectionToClient);
        StartCoroutine(DelayedAction(0.5f, state, () => ServerDistributeRoles(state)));
    }

    [Server]
    private void ServerDistributeRoles(RoomGameState state)
    {
        state.Room.Phase = GamePhase.RoleDistribution;

        foreach (var gp in state.GamePlayers)
            gp.TargetSendRole(gp.connectionToClient, gp.RoomIndex == state.DanyIndex);

        StartCoroutine(DelayedAction(3f, state, () => ServerStartNextTurn(state)));
    }

    #endregion

    #region Ходы

    [Server]
    private void ServerSweepAndStartNextTurn(RoomGameState state)
    {
        foreach (NetworkPlayer np in state.Room.Players)
            TargetSweepTableCards(np.connectionToClient);
        StartCoroutine(DelayedAction(Card.SweepDuration + 0.3f, state, () => ServerStartNextTurn(state)));
    }

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
            if (gp.RoomIndex == state.CurrentIndex) gp.Role = Role.Active;
            else if (gp.RoomIndex == state.DecisiveIndex) gp.Role = Role.Decisive;
            else gp.Role = Role.Waiting;
        }

        ServerDrawCardsForPlayers(state);
        ServerDrawIdeasCard(state);
        foreach (NetworkPlayer np in state.Room.Players)
            TargetStartTurn(np.connectionToClient, state.Room.RoomCode);

        state.TurnStartTime = Time.time;
        int capturedTurnIdx = state.CurrentIndex;
        StartCoroutine(DelayedAction(turnTime, state, () =>
        {
            if (state.Room.Phase == GamePhase.TurnInProgress && state.CurrentIndex == capturedTurnIdx)
            {
                GamePlayer gp = state.GamePlayers.Find(p => p.RoomIndex == capturedTurnIdx);
                if (gp != null) ServerOnPlayerFinishedTurn(gp);
            }
        }));
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
                cardObj.GetComponent<NetworkMatch>().matchId = state.MatchId;

                gp.HandCardNetIds.Add(netCard.netId);
                gp.TargetAddCardToHand(gp.connectionToClient, netCard.netId, canInteract: true);
            }
            TargetPopdownHand(gp.connectionToClient);
            TargetPopupHand(gp.connectionToClient);
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

    [Server]
    public void ServerOnPlayerFinishedTurn(GamePlayer gPlayer)
    {
        if (!TryFindState(gPlayer, out var state)) return;
        if (gPlayer.RoomIndex != state.CurrentIndex) return;
        if (state.Room.Phase != GamePhase.TurnInProgress) return;

        state.Room.Phase = GamePhase.Discussion;
        state.DiscussionStartTime = Time.time;
        TargetToggleGameReadyToggle(gPlayer.connectionToClient, false);
        gPlayer.TargetClearHand(gPlayer.connectionToClient);

        // Enable word buttons for decisive + send popup to everyone
        float delay = 6f;
        foreach (GamePlayer gp in state.GamePlayers) {
            var np = gp.Owner;
            TargetToggleCardsInteraction(np.connectionToClient, false);
            TargetStartDiscussion(np.connectionToClient, state.Room.RoomCode);
            if (gp.Role == Role.Decisive)
            {
                gp.TargetShowGuessPanel(np.connectionToClient, state.CurrentIdeasCard);
                gp.TargetShowLocGamePopup(np.connectionToClient, "gameUI.guess.des", delay);

                // Remind to choose a word
                var savedScore = (state.Room.DanyScore, state.Room.PersonalitiesScore);
                StartCoroutine(DelayedAction(discussionTime - 10f, state, () =>
                {
                    if (savedScore == (state.Room.DanyScore, state.Room.PersonalitiesScore))
                        gp.TargetShowLocGamePopup(np.connectionToClient, "gameUI.guess.remind", 2f);
                }));
            }
            else if (gp.Role == Role.Active)
            {
                gp.TargetShowNickLocGamePopup(np.connectionToClient,
                    state.GamePlayers[state.DecisiveIndex].LobbyNumber,
                    "gameUI.guess.act", delay);
            }
            else
            {
                gp.TargetShowNickLocGamePopup(np.connectionToClient,
                    state.GamePlayers[state.DecisiveIndex].LobbyNumber,
                    "gameUI.guess.others", delay);
            }
        }

        int capturedDecisiveIdx = state.DecisiveIndex;
        StartCoroutine(DelayedAction(discussionTime, state, () =>
        {
            if (state.Room.Phase == GamePhase.Discussion && state.DecisiveIndex == capturedDecisiveIdx)
                ServerHandleDiscussionTimeout(state);
        }));
    }

    [Server]
    public void ServerOnTurnTimerEnded(NetworkPlayer player, string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        if (state.Room.Phase != GamePhase.TurnInProgress) return;
        if (player == null || !player.IsHost) return;
        if (player.CurrentRoomCode != roomCode) return;
        if (Time.time - state.TurnStartTime < turnTime - TimerGracePeriod) return;
        GamePlayer gp = state.GamePlayers.Find(p => p.RoomIndex == state.CurrentIndex);
        if (gp != null) ServerOnPlayerFinishedTurn(gp);
    }

    [Server]
    public void ServerOnDecisiveTimerEnded(NetworkPlayer player, string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        if (state.Room.Phase != GamePhase.Discussion) return;
        if (player == null || !player.IsHost) return;
        if (player.CurrentRoomCode != roomCode) return;
        if (Time.time - state.DiscussionStartTime < discussionTime - TimerGracePeriod) return;
        ServerHandleDiscussionTimeout(state);
    }

    [Server]
    private void ServerHandleDiscussionTimeout(RoomGameState state)
    {
        if (state.Room.Phase != GamePhase.Discussion) return;
        state.Room.DanyScore++;
        state.Room.Phase = GamePhase.WordReveal;

        float delay = 8f;
        foreach (var gp in state.GamePlayers) {
            gp.TargetShowLocGamePopup(gp.connectionToClient, "lostByTimeout", delay);
            gp.TargetHighLightGuess(gp.connectionToClient, -1, state.SecretWordIndex);
        }

        StartCoroutine(DelayedAction(delay, state, () =>
        {
            if (!ServerCheckGameEnd(state))
                ServerSweepAndStartNextTurn(state);
        }));
    }

    [Server]
    public void ServerOnWordGuessed(GamePlayer gplayer, int wordIndex)
    {
        if (!TryFindState(gplayer, out var state)) return;
        if (gplayer.RoomIndex != state.DecisiveIndex) return;
        if (state.Room.Phase != GamePhase.Discussion) return;

        state.Room.Phase = GamePhase.WordReveal;

        string guesskey;
        if (wordIndex == state.SecretWordIndex)
        {
            state.Room.PersonalitiesScore++;
            guesskey = "chat.correctGuess";
        }
        else
        {
            state.Room.DanyScore++;
            guesskey = "chat.wrongGuess";
        }

        float delay = 8f;
        foreach (var gp in state.GamePlayers) {
            gp.TargetShowLocGamePopup(gp.connectionToClient, guesskey, delay);
            gp.TargetHighLightGuess(gp.connectionToClient, state.SecretWordIndex, wordIndex);
        }

        StartCoroutine(DelayedAction(delay, state, () =>
        {
            if (!ServerCheckGameEnd(state))
                ServerSweepAndStartNextTurn(state);
        }));
    }

    [Server]
    public void ServerOnVoteReceived(NetworkPlayer voter, int suspectedLobbyNumber)
    {
        if (string.IsNullOrEmpty(voter.CurrentRoomCode)) return;
        if (!_rooms.TryGetValue(voter.CurrentRoomCode, out var state)) return;
        if (state.VotingResolved) return;
        state.Votes[voter.Number] = suspectedLobbyNumber;
        if (state.Votes.Count >= state.GamePlayers.Count)
        {
            state.VotingResolved = true;
            ServerResolveVotes(state);
        }
    }

    [Server]
    public void ServerOnVotingTimerEnded(NetworkPlayer player, string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        if (state.Room.Phase != GamePhase.FinalRound) return;
        if (!player.IsHost) return;
        if (player.CurrentRoomCode != roomCode) return;
        if (Time.time - state.VotingStartTime < votingTime - TimerGracePeriod) return;
        if (state.VotingResolved) return;
        state.VotingResolved = true;
        ServerResolveVotes(state);
    }

    [Server]
    private void ServerResolveVotes(RoomGameState state)
    {
        var tally = new Dictionary<int, int>();
        var votersByCandidate = new Dictionary<int, List<int>>();
        foreach (var gp in state.GamePlayers)
        {
            tally[gp.LobbyNumber] = 0;
            votersByCandidate[gp.LobbyNumber] = new List<int>();
        }
        foreach (var kvp in state.Votes)
        {
            if (tally.ContainsKey(kvp.Value)) tally[kvp.Value]++;
            if (votersByCandidate.ContainsKey(kvp.Value)) votersByCandidate[kvp.Value].Add(kvp.Key);
        }

        int maxVotes = 0;
        foreach (int cnt in tally.Values) if (cnt > maxVotes) maxVotes = cnt;
        var topVoted = new List<int>();
        foreach (var kvp in tally) if (kvp.Value == maxVotes) topVoted.Add(kvp.Key);

        var suspects = new List<int>();
        var voterCounts = new List<int>();
        var votersFlat = new List<int>();
        foreach (var gp in state.GamePlayers)
        {
            suspects.Add(gp.LobbyNumber);
            var voters = votersByCandidate[gp.LobbyNumber];
            voterCounts.Add(voters.Count);
            votersFlat.AddRange(voters);
        }
        foreach (NetworkPlayer np in state.Room.Players)
            TargetShowVoteResults(np.connectionToClient, suspects, voterCounts, votersFlat);

        if (topVoted.Count == 1)
        {
            bool dannyFound = topVoted[0] == state.DanyLobbyNumber;
            StartCoroutine(DelayedAction(10f, state, () => ServerEndGame(state, dannyWins: !dannyFound)));
        }
        else
        {
            StartCoroutine(DelayedAction(10f, state, () =>
            {
                state.Votes.Clear();
                state.VotingResolved = false;
                foreach (NetworkPlayer np in state.Room.Players)
                    TargetStartTieVote(np.connectionToClient, topVoted);
            }));
        }
    }

    [Server]
    private void ServerEndGame(RoomGameState state, bool dannyWins)
    {
        state.Room.Phase = GamePhase.GameEnd;
        foreach (NetworkPlayer np in state.Room.Players) 
        {
            np.GamePlayer.Role = Role.Waiting;
            TargetGameEnded(np.connectionToClient, dannyWins, state.DanyLobbyNumber, np.Number == state.DanyLobbyNumber);
        }
    }

    [Server]
    private bool ServerCheckGameEnd(RoomGameState state)
    {
        if (state.Room.PersonalitiesScore >= 6)
        {
            ServerEndGame(state, false);
            return true;
        }
        if (state.Room.DanyScore >= 3 || !PicturesDeck.Instance.EnoughCardsToDraw())
        {
            state.Room.Phase = GamePhase.FinalRound;
            state.VotingStartTime = Time.time;
            var numbers = state.GamePlayers.ConvertAll(gp => gp.LobbyNumber);
            foreach (NetworkPlayer np in state.Room.Players)
            {
                np.GamePlayer.Role = Role.Waiting;
                TargetStartFinalRound(np.connectionToClient, state.DanyLobbyNumber, state.Room.RoomCode, numbers);
            }
            StartCoroutine(DelayedAction(votingTime, state, () =>
            {
                if (state.Room.Phase == GamePhase.FinalRound && !state.VotingResolved)
                {
                    state.VotingResolved = true;
                    ServerResolveVotes(state);
                }
            }));
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
        state.VotingResolved = false;
        state.PlayersReturnedToLobby.Clear();
        state.CurrentIdeasCard = null;
        state.CurrentIndex = -1;
        state.DecisiveIndex = -1;

        state.Room.IsInProgress = false;
        state.Room.Phase = GamePhase.Lobby;
        state.Room.PersonalitiesScore = 0;
        state.Room.DanyScore = 0;

        foreach (NetworkPlayer np in state.Room.Players)
            TargetReturnToLobby(np.connectionToClient);
    }

    #endregion

    #region Вспомогательные методы

    [Server]
    public void ServerHandleChatMessage(string roomCode, int senderNum, string text)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        ServerSaveChatMessage(roomCode, senderNum, text);
        foreach (NetworkPlayer np in state.Room.Players)
            np.TargetReceiveChatMessage(np.connectionToClient, senderNum, text);
    }

    [Server]
    public void ServerSaveChatMessage(string roomCode, int senderNum, string text)
    {
        if (!_rooms.TryGetValue(roomCode, out var state)) return;
        state.ChatSenderNums.Add(senderNum);
        state.ChatTexts.Add(text);
        if (state.ChatSenderNums.Count > 50)
        {
            state.ChatSenderNums.RemoveAt(0);
            state.ChatTexts.RemoveAt(0);
        }
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

    #region Рассылка событий игрокам комнаты

    [TargetRpc]
    private void TargetSweepTableCards(NetworkConnectionToClient conn)
    => PlayingCardsTable.Instance.SweepAllTableCards();

    [TargetRpc]
    private void TargetPopupHand(NetworkConnectionToClient conn)
        => PlayingCardsTable.Instance.PopupHand();

    [TargetRpc]
    private void TargetPopdownHand(NetworkConnectionToClient conn)
        => PlayingCardsTable.Instance.PopdownHand();

    [TargetRpc]
    private void TargetRoomUpdated(NetworkConnectionToClient conn)
        => LobbyManager.Instance.RefreshRoomPanel();

    [TargetRpc]
    private void TargetGameStarted(NetworkConnectionToClient conn)
        => LobbyManager.Instance.OnGameStarted();

    [TargetRpc]
    private void TargetStartTurn(NetworkConnectionToClient conn, string roomCode)
        => TimerUI.Instance.StartTimer(turnTime, null);

    [TargetRpc]
    private void TargetStartDiscussion(NetworkConnectionToClient conn, string roomCode)
        => TimerUI.Instance.StartTimer(discussionTime, null);

    [TargetRpc]
    private void TargetToggleCardsInteraction(NetworkConnectionToClient conn, bool active)
        => PlayingCardsTable.Instance.ToggleInteractions(active);

    [TargetRpc]
    private void TargetToggleGameReadyToggle(NetworkConnectionToClient conn, bool interactable)
        => LobbyManager.Instance.ToggleGameReadyToggle(interactable);

    [TargetRpc]
    private void TargetStartFinalRound(NetworkConnectionToClient conn, int danyLobbyNumber, string code, List<int> lobbyNumbers)
        => NetworkFinalRoundManager.Instance.StartFinalRound(danyLobbyNumber, code, lobbyNumbers);

    [TargetRpc]
    private void TargetShowVoteResults(NetworkConnectionToClient conn, List<int> suspects, List<int> voterCounts, List<int> votersFlat)
        => NetworkFinalRoundManager.Instance.ShowVoteResults(suspects, voterCounts, votersFlat);

    [TargetRpc]
    private void TargetStartTieVote(NetworkConnectionToClient conn, List<int> tiedLobbyNumbers)
        => NetworkFinalRoundManager.Instance.StartTieVote(tiedLobbyNumbers);

    [TargetRpc]
    private void TargetGameEnded(NetworkConnectionToClient conn, bool danyWins, int dany, bool wasDany)
    {
        LocalPlayerStats.Instance.AddGame(wasDany, danyWins);
        LobbyManager.Instance.ShowGameEndScreen(danyWins, dany);
        TimerUI.Instance.StopTimer();
    }

    [TargetRpc]
    private void TargetShowAbortionPanel(NetworkConnectionToClient conn, string message) 
    { 
        LobbyManager.Instance.ShowAbortionScreen(message);
        TimerUI.Instance.StopTimer();
    }

    [TargetRpc]
    private void TargetReturnToLobby(NetworkConnectionToClient conn)
    {
        PlayingCardsTable.Instance.ClearTable();
        PlayingCardsTable.Instance.ClearHand();
        ScoreUI.Instance.ResetScore();
        GamePopup.Instance.Hide();
        PopupUI.Instance.Hide();
        LobbyManager.Instance.ShowLobby();
        TimerUI.Instance.StopTimer();
    }

    #endregion
}

