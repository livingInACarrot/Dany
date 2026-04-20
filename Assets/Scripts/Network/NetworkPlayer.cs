using Mirror;
using UnityEngine;

/// <summary>
/// Сетевой экземпляр игрока. Существует на протяжении всего подключения —
/// от входа на сервер до дисконнекта. Содержит только лобби/транспортные данные.
/// Игровые поля (роль, IsDanny, карты) вынесены в GamePlayer.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    /// <summary>Уникальный номер — connectionId, назначается сервером.</summary>
    [SyncVar(hook = nameof(OnNumberChanged))]
    public int Number;

    [SyncVar(hook = nameof(OnCountryChanged))]
    public string Country;

    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool IsReady;

    [SyncVar]
    public bool IsHost;

    /// <summary>Код комнаты, в которой находится игрок (пусто — в главном лобби).</summary>
    [SyncVar]
    public string CurrentRoomCode;

    /// <summary>netId соответствующего GamePlayer (0 — вне игры).</summary>
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

    private void Start()
    {
        if (!isLocalPlayer) return;
        string country = PlayerPrefs.GetString("PlayerCountry", "Unknown");
        CmdUpdateCountry(country);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnPlayerAdded?.Invoke(this);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        OnPlayerRemoved?.Invoke(this);
    }

    // =====================================================
    // Commands
    // =====================================================

    [Command]
    public void CmdUpdateCountry(string country) => Country = country;

    [Command]
    public void CmdSetReady(bool ready) => IsReady = ready;

    [Command]
    public void CmdPlaceCard(uint cardNetId, Vector2 position, Quaternion rotation, Vector3 scale)
    {
        RpcUpdateCardPosition(cardNetId, position, rotation, scale);
    }

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

    /// <summary>
    /// Голосование финального раунда. suspectedIndex — RoomIndex подозреваемого.
    /// Делегируется NetworkGameManager, который знает контекст комнаты.
    /// </summary>
    [Command]
    public void CmdVote(int suspectedRoomIndex)
    {
        NetworkGameManager.Instance?.ServerOnVoteReceived(this, suspectedRoomIndex);
    }

    // =====================================================
    // SyncVar hooks
    // =====================================================

    private void OnNumberChanged(int _, int __)  => OnDataChanged?.Invoke();
    private void OnCountryChanged(string _, string __) => OnDataChanged?.Invoke();
    private void OnReadyChanged(bool _, bool __)  => OnDataChanged?.Invoke();
}
