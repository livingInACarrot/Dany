using Mirror;
using UnityEngine;

/// <summary>
/// Сетевой экземпляр игрока. Существует на протяжении всего подключения.
/// Содержит только лобби/транспортные данные; игровые поля — в GamePlayer.
/// Все Commands и TargetRpc проходят через этот объект, у которого есть
/// корректный netId и authority, в отличие от scene-объекта NetworkGameManager.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNumberChanged))]
    public int Number;

    [SyncVar(hook = nameof(OnCountryChanged))]
    public string Country;

    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool IsReady;

    [SyncVar]
    public bool IsHost;

    [SyncVar]
    public string CurrentRoomCode;

    [SyncVar]
    public uint GamePlayerNetId;

    public event System.Action OnDataChanged;

    public static event System.Action<NetworkPlayer> OnPlayerAdded;
    public static event System.Action<NetworkPlayer> OnPlayerRemoved;

    // =====================================================
    // Lifecycle
    // =====================================================

    public override void OnStartServer()
    {
        base.OnStartServer();
        Number = connectionToClient.connectionId;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnPlayerAdded?.Invoke(this);
    }

    /// <summary>
    /// Гарантированно вызывается только для локального игрока и только после
    /// того как Mirror полностью настроил объект (isLocalPlayer = true, netId задан).
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        string country = PlayerPrefs.GetString("PlayerCountry", "Unknown");
        CmdUpdateCountry(country);
        // Уведомляем LobbyManager — время отправить отложенный запрос создания/входа в комнату
        LobbyManager.Instance?.OnLocalPlayerSpawned();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        OnPlayerRemoved?.Invoke(this);
    }

    // =====================================================
    // Commands — управление комнатой
    // Команды на NetworkPlayer, а не на NetworkGameManager, потому что
    // NetworkPlayer имеет корректный netId и authority; scene-объект
    // NetworkGameManager требует NetworkIdentity для Commands.
    // =====================================================

    [Command]
    public void CmdCreateRoom()
        => NetworkGameManager.Instance?.ServerCreateRoom(this);

    [Command]
    public void CmdJoinRoom(string code)
        => NetworkGameManager.Instance?.ServerJoinRoom(code, this);

    [Command]
    public void CmdLeaveRoom()
        => NetworkGameManager.Instance?.ServerLeaveRoom(this);

    [Command]
    public void CmdStartGame()
        => NetworkGameManager.Instance?.ServerRequestStartGame(this);

    [Command]
    public void CmdReturnToLobby()
        => NetworkGameManager.Instance?.ServerRequestReturnToLobby(this);

    // =====================================================
    // Commands — состояние игрока
    // =====================================================

    [Command]
    public void CmdUpdateCountry(string country) => Country = country;

    [Command]
    public void CmdSetReady(bool ready) => IsReady = ready;

    [Command]
    public void CmdPlaceCard(uint cardNetId, Vector2 position, Quaternion rotation, Vector3 scale)
        => RpcUpdateCardPosition(cardNetId, position, rotation, scale);

    [ClientRpc]
    private void RpcUpdateCardPosition(uint cardNetId, Vector2 position, Quaternion rotation, Vector3 scale)
    {
        if (NetworkClient.spawned.TryGetValue(cardNetId, out NetworkIdentity identity))
            identity?.GetComponent<Card>()?.ChangePosition(position, rotation, scale);
    }

    [Command]
    public void CmdSendChatMessage(string message, int num)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        RpcReceiveChatMessage(Loc.Nick(num), message);
    }

    [ClientRpc]
    private void RpcReceiveChatMessage(string sender, string message)
        => NetworkChat.Instance?.AddMessage(sender, message);

    [Command]
    public void CmdVote(int suspectedRoomIndex)
        => NetworkGameManager.Instance?.ServerOnVoteReceived(this, suspectedRoomIndex);

    // =====================================================
    // TargetRpc — ответы сервера конкретному игроку
    // =====================================================

    [TargetRpc]
    public void TargetRoomCreated(NetworkConnectionToClient conn, string code)
        => LobbyManager.Instance?.OnRoomCreated(code);

    [TargetRpc]
    public void TargetJoinedRoom(NetworkConnectionToClient conn, string code)
        => LobbyManager.Instance?.OnJoinedRoom(code);

    [TargetRpc]
    public void TargetRoomError(NetworkConnectionToClient conn, string error)
        => LobbyManager.Instance?.OnRoomError(error);

    // =====================================================
    // SyncVar hooks
    // =====================================================

    private void OnNumberChanged(int _, int __)    => OnDataChanged?.Invoke();
    private void OnCountryChanged(string _, string __) => OnDataChanged?.Invoke();
    private void OnReadyChanged(bool _, bool __)   => OnDataChanged?.Invoke();
}
