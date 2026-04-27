using Mirror;
using UnityEngine;

/// <summary>
/// Игровой экземпляр игрока. Спавнится при старте игры и содержит всё, что нужно только во время игры.
/// </summary>
public class GamePlayer : NetworkBehaviour
{
    // индекс игрока внутри списка GamePlayers в RoomGameState
    [SyncVar] public int RoomIndex;

    // Лобби-номер NetworkPlayer (для ника)
    [SyncVar] public int LobbyNumber;

    [SyncVar] public bool IsDanny;

    [SyncVar(hook = nameof(OnRoleChanged))]
    public Role Role = Role.Waiting;

    [SyncVar] public bool HasFinishedTurn;

    // netId NetworkPlayer
    [SyncVar] public uint OwnerNetId;

    public readonly SyncList<uint> HandCardNetIds = new();

    public static event System.Action<GamePlayer> OnSpawned;
    public static event System.Action<GamePlayer> OnDespawned;

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
    public void TargetSendRole(NetworkConnectionToClient conn, bool isDanny)
    {
        LobbyManager.Instance.OnRoleAssigned(isDanny);
    }

    [TargetRpc]
    public void TargetShowActiveView(NetworkConnectionToClient conn, IdeasCard card, int wordIndex)
    {
        IdeasCardUI.Instance.ShowForActiveRole(card, wordIndex);
    }

    [TargetRpc]
    public void TargetShowOthersView(NetworkConnectionToClient conn, IdeasCard card)
    {
        IdeasCardUI.Instance.ShowForOthers(card);
    }

    [TargetRpc]
    public void TargetAddCardToHand(NetworkConnectionToClient conn, uint cardNetId)
    {
        if (NetworkClient.spawned.TryGetValue(cardNetId, out NetworkIdentity identity))
        {
            Card card = identity.GetComponent<Card>();
            if (card != null) PlayingCardsTable.Instance.ReturnCardToHand(card);
        }
    }

    [TargetRpc]
    public void TargetShowGuessPanel(NetworkConnectionToClient conn, IdeasCard card)
    {
        IdeasCardUI.Instance.ShowGuessPanel(card);
    }

    private void OnRoleChanged(Role _, Role newRole)
    {
        if (isLocalPlayer)
            LobbyManager.Instance.RefreshRoomPanel();
    }
}
