
[System.Serializable]
public class Player
{
    public bool IsHost;
    public bool IsDanny;
    public bool IsReady;
    public int Number;
    public Role Role;
    public PlayerData Data;

    private static int nextNumber = 1;
    private static readonly object lockObject = new();

    public Player(PlayerData playerData, bool isHost = false, bool isDanny = false, bool isReady = false)
    {
        IsHost = isHost;
        IsDanny = isDanny;
        IsReady = isReady;
        Data = playerData;
        Role = Role.Waiting;
        lock (lockObject)
        {
            Number = nextNumber++;
        }
    }
}
