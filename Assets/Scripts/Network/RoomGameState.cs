using System.Collections.Generic;

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
    public readonly List<int> ChatSenderNums = new();
    public readonly List<string> ChatTexts = new();

    public RoomGameState(GameRoom room) => Room = room;
}