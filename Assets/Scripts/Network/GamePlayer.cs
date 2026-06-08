using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar] public int RoomIndex;

    [SyncVar] public int LobbyNumber;

    [SyncVar(hook = nameof(OnChangedRole))]
    public Role Role = Role.Waiting;

    [SyncVar] public bool HasFinishedTurn;

    [SyncVar] public uint OwnerNetId;

    public readonly SyncList<uint> HandCardNetIds = new();

    public NetworkPlayer Owner
    {
        get
        {
            if (NetworkServer.spawned.TryGetValue(OwnerNetId, out var id)) return id.GetComponent<NetworkPlayer>();
            if (NetworkClient.spawned.TryGetValue(OwnerNetId, out id)) return id.GetComponent<NetworkPlayer>();
            return null;
        }
    }

    public static GamePlayer Local
    {
        get
        {
            if (NetworkClient.localPlayer == null) return null;
            var np = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
            if (np == null) return null;
            if (!NetworkClient.spawned.TryGetValue(np.GamePlayerNetId, out var identity)) return null;
            return identity.GetComponent<GamePlayer>();
        }
    }

    public static event System.Action<GamePlayer> OnSpawned;
    public static event System.Action<GamePlayer> OnDespawned;
    public static event System.Action<GamePlayer> OnRoleChanged;

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnSpawned?.Invoke(this);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        OnDespawned?.Invoke(this);
    }

    [Command]
    public void CmdFinishTurn()
    {
        NetworkGameManager.Instance.ServerOnPlayerFinishedTurn(this);
    }

    [Command]
    public void CmdWordGuessed(int wordIndex)
    {
        NetworkGameManager.Instance.ServerOnWordGuessed(this, wordIndex);
    }

    [TargetRpc]
    public void TargetSendRole(NetworkConnectionToClient conn, bool isDany)
    {
        LobbyManager.Instance.OnRoleAssigned(isDany);
    }

    [TargetRpc]
    public void TargetShowActiveView(NetworkConnectionToClient conn, string cardKey, int wordIndex)
    {
        IdeasCardUI.Instance.ShowForActiveRole(cardKey, wordIndex);
    }

    [TargetRpc]
    public void TargetShowOthersView(NetworkConnectionToClient conn, string cardKey)
    {
        IdeasCardUI.Instance.ShowForOthers(cardKey);
    }

    [TargetRpc]
    public void TargetHighLightGuess(NetworkConnectionToClient conn, int corr, int guess)
    {
        IdeasCardUI.Instance.HighLightGuess(corr, guess);
    }

    [TargetRpc]
    public void TargetAddCardToHand(NetworkConnectionToClient conn, uint cardNetId, bool canInteract)
    {
        if (!NetworkClient.spawned.TryGetValue(cardNetId, out NetworkIdentity identity)) return;
        Card card = identity.GetComponent<Card>();
        if (card == null) return;
        PlayingCardsTable.Instance.ReturnCardToHand(card);
        identity.GetComponent<Image>().raycastTarget = canInteract;
        identity.GetComponent<Button>().interactable = canInteract;
    }

    [TargetRpc]
    public void TargetClearHand(NetworkConnectionToClient conn)
    {
        PlayingCardsTable.Instance.ClearHand();
    }

    [TargetRpc]
    public void TargetShowGuessPanel(NetworkConnectionToClient conn, IdeasCard card)
    {
        IdeasCardUI.Instance.ShowGuessPanel(card);
    }

    [TargetRpc]
    public void TargetShowLocGamePopup(NetworkConnectionToClient conn, string locKey, float time)
    {
        GamePopup.Instance.Show(Loc.Text(locKey), time);
    }

    [TargetRpc]
    public void TargetShowNickLocGamePopup(NetworkConnectionToClient conn, int playerNumber, string locKey, float time)
    {
        GamePopup.Instance.Show($"{Loc.Nick(playerNumber)} {Loc.Text(locKey)}", time);
    }

    [TargetRpc]
    public void TargetShowGamePopup(NetworkConnectionToClient conn, string text, float time)
    {
        GamePopup.Instance.Show(text, time);
    }

    private void OnChangedRole(Role _, Role newRole)
    {
        OnRoleChanged.Invoke(this);
        bool isOwnerLocal = NetworkClient.localPlayer != null && OwnerNetId == NetworkClient.localPlayer.netId;
        if (isOwnerLocal)
        {
            LobbyManager.Instance.RefreshRoomPanel();
            LobbyManager.Instance.SetGameReadyVisible(newRole == Role.Active);
            if (VoiceController.Instance != null)
                VoiceController.Instance.TurnMuted = newRole == Role.Active;
            ChatUI.Instance?.SetInputEnabled(newRole != Role.Active);
        }
    }
}
