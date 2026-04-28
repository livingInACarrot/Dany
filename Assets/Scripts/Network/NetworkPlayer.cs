using Mirror;
using UnityEngine;

/// <summary>
/// Сетевой экземпляр игрока.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNumberChanged))]
    public int Number;

    [SyncVar(hook = nameof(OnCountryChanged))]
    public string Country;

    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool IsReady;

    [SyncVar(hook = nameof(OnIsHostChanged))]
    public bool IsHost;

    [SyncVar(hook = nameof(OnCurrentRoomCodeChanged))]
    public string CurrentRoomCode;

    [SyncVar]
    public uint GamePlayerNetId;

    public event System.Action OnDataChanged;

    public static event System.Action<NetworkPlayer> OnPlayerAdded;
    public static event System.Action<NetworkPlayer> OnPlayerRemoved;

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnPlayerAdded?.Invoke(this);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        string country = PlayerPrefs.GetString("PlayerCountry", "Unknown");
        CmdUpdateCountry(country);
        LobbyManager.Instance?.OnLocalPlayerSpawned();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        OnPlayerRemoved?.Invoke(this);
        //VoiceChat.Instance?.RemoveSource(netId);
    }

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

    [Command]
    public void CmdSetRoomPrivacy(bool isPrivate)
        => NetworkGameManager.Instance?.ServerSetRoomPrivacy(this, isPrivate);

    [Command]
    public void CmdUpdateCountry(string country) => Country = country;

    [Command]
    public void CmdSetReady(bool ready) => IsReady = ready;

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
    public void CmdVote(int suspectedLobbyNumber)
        => NetworkGameManager.Instance?.ServerOnVoteReceived(this, suspectedLobbyNumber);

    [Command]
    public void CmdTurnTimerEnded(string roomCode)
        => NetworkGameManager.Instance?.ServerOnTurnTimerEnded(this, roomCode);


    [Command]
    public void CmdSendVoice(byte[] pcm)
        => RpcReceiveVoice(netId, pcm);

    [ClientRpc]
    private void RpcReceiveVoice(uint senderNetId, byte[] pcm)
    {
        if (isLocalPlayer) return;
        //VoiceChat.Instance?.PlayRemoteAudio(senderNetId, pcm);
    }


    [TargetRpc]
    public void TargetRoomCreated(NetworkConnectionToClient conn, string code)
        => LobbyManager.Instance?.OnRoomCreated(code);

    [TargetRpc]
    public void TargetJoinedRoom(NetworkConnectionToClient conn, string code)
        => LobbyManager.Instance?.OnJoinedRoom(code);

    [TargetRpc]
    public void TargetRoomError(NetworkConnectionToClient conn, string error)
        => LobbyManager.Instance?.OnRoomError(error);


    private void OnNumberChanged(int _, int __)        => OnDataChanged?.Invoke();
    private void OnCountryChanged(string _, string __)  => OnDataChanged?.Invoke();
    private void OnReadyChanged(bool _, bool __)
    {
        OnDataChanged?.Invoke();
        LobbyManager.Instance?.RefreshRoomPanel();
    }
    private void OnIsHostChanged(bool _, bool __)       => LobbyManager.Instance?.RefreshRoomPanel();
    private void OnCurrentRoomCodeChanged(string _, string __) => LobbyManager.Instance?.RefreshRoomPanel();
}
