using System;
using System.Collections.Generic;
using Mirror;

public class GameRoom : NetworkBehaviour
{
    public const int MinPlayers = 3;
    public const int DefaultMaxPlayers = 8;

    [SyncVar] public string RoomCode;
    [SyncVar(hook = nameof(OnPrivacyChanged))] public bool IsPrivate;
    [SyncVar] public int PlayerCount;
    [SyncVar] public int MaxPlayers = DefaultMaxPlayers;
    [SyncVar] public bool IsInProgress;

    [SyncVar] public GamePhase Phase = GamePhase.Lobby;
    [SyncVar] public int PersonalitiesScore;
    [SyncVar] public int DanyScore;

    public static readonly List<GameRoom> All = new();
    public static event Action OnRoomListChanged;

    private readonly List<NetworkPlayer> _players = new();
    private int _hostConnectionId = -1;

    public IReadOnlyList<NetworkPlayer> Players => _players;
    public bool CanStart => PlayerCount >= MinPlayers && !IsInProgress;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsPrivate && !All.Contains(this)) All.Add(this);
        OnRoomListChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        All.Remove(this);
        OnRoomListChanged?.Invoke();
    }

    private void OnPrivacyChanged(bool _, bool isNowPrivate)
    {
        if (isNowPrivate)
            All.Remove(this);
        else if (!All.Contains(this))
            All.Add(this);
        OnRoomListChanged?.Invoke();
    }

    [Server]
    public bool TryAddPlayer(NetworkPlayer player)
    {
        if (PlayerCount >= MaxPlayers || IsInProgress) return false;
        _players.Add(player);
        PlayerCount = _players.Count;
        player.Number = PlayerCount;
        if (_hostConnectionId == -1)
        {
            _hostConnectionId = player.connectionToClient.connectionId;
            player.IsHost = true;
        }
        return true;
    }

    [Server]
    public void RemovePlayer(NetworkPlayer player)
    {
        bool wasHost = player.connectionToClient.connectionId == _hostConnectionId;
        _players.Remove(player);
        player.Number = 0;
        PlayerCount = _players.Count;
        if (wasHost) MigrateHost();
    }

    [Server]
    public void MigrateHost()
    {
        if (_players.Count == 0) { _hostConnectionId = -1; return; }
        foreach (var p in _players) p.IsHost = false;
        _hostConnectionId = _players[0].connectionToClient.connectionId;
        _players[0].IsHost = true;
        RpcHostMigrated(_players[0].netId);
    }

    [ClientRpc]
    private void RpcHostMigrated(uint newHostNetId)
    {
        LobbyManager.Instance.OnHostMigrated(newHostNetId);
    }
}
