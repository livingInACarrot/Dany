using System;
using System.Collections.Generic;

public class RoomGameState
{
    public GameRoom Room;
    public Guid MatchId;
    public int CurrentIndex = -1;
    public int DecisiveIndex = -1;
    public int DanyIndex;
    public int DanyLobbyNumber;
    public List<GamePlayer> GamePlayers = new();
    public IdeasCard CurrentIdeasCard;
    public int SecretWordIndex;
    public Dictionary<int, int> Votes = new();
    public bool VotingResolved;
    public readonly HashSet<int> PlayersReturnedToLobby = new();
    public readonly List<int> ChatSenderNums = new();
    public readonly List<string> ChatTexts = new();

    // Server-side timer tracking: used to validate that host-reported timer endings
    // actually happened after the expected duration elapsed.
    public float TurnStartTime;
    public float DiscussionStartTime;
    public float VotingStartTime;

    public int GameId;
    public int TieVoteCount;

    public RoomGameState(GameRoom room) => Room = room;
}