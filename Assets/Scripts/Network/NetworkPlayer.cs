using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPlayerNumberChanged))]
    public int playerNumber;

    [SyncVar(hook = nameof(OnPlayerCountryChanged))]
    public string playerCountry;

    [SyncVar(hook = nameof(OnIsReadyChanged))]
    public bool isReady;

    [SyncVar(hook = nameof(OnIsDannyChanged))]
    public bool isDanny;

    [SyncVar(hook = nameof(OnRoleChanged))]
    public Role role;

    [SyncVar]
    public bool isHost;

    public readonly SyncList<uint> handCardNetIds = new SyncList<uint>();

    public System.Action<int, string, bool, bool, Role> OnPlayerDataChanged;
    public static event System.Action<NetworkPlayer> OnPlayerAdded;
    public static event System.Action<NetworkPlayer> OnPlayerRemoved;

    private void Start()
    {
        if (isLocalPlayer)
        {
            string country = PlayerPrefs.GetString("PlayerCountry", "Россия");
            CmdUpdatePlayerData(country);
        }
    }

    [Command]
    private void CmdUpdatePlayerData(string country)
    {
        playerCountry = country;
        playerNumber = connectionToClient.connectionId;
    }

    [Command]
    public void CmdSetReady(bool ready)
    {
        isReady = ready;
    }

    [Command]
    public void CmdStartGame()
    {
        if (isServer)
        {
            NetworkGameManager.Instance.StartNetworkGame();
        }
    }

    [Command]
    public void CmdPlaceCard(uint cardNetId, Vector2 position, Quaternion rotation, Vector3 scale)
    {
        if (!isOwned) return;
        RpcUpdateCardPosition(cardNetId, position, rotation, scale);
    }

    [ClientRpc]
    private void RpcUpdateCardPosition(uint cardNetId, Vector2 position, Quaternion rotation, Vector3 scale)
    {
        if (NetworkClient.spawned.TryGetValue(cardNetId, out NetworkIdentity identity))
        {
            Card card = identity.GetComponent<Card>();
            if (card != null)
            {
                card.ChangePosition(position, rotation, scale);
            }
        }
    }

    [Command]
    public void CmdSendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        string playerName = $"Игрок {playerNumber}";
        RpcReceiveChatMessage(playerName, message);
    }

    [ClientRpc]
    private void RpcReceiveChatMessage(string sender, string message)
    {
        if (NetworkChat.Instance != null)
        {
            NetworkChat.Instance.AddMessage(sender, message);
        }
    }

    [Command]
    public void CmdVote(int suspectedPlayerNumber)
    {
        NetworkGameManager.Instance.ProcessVote(playerNumber, suspectedPlayerNumber);
    }

    [Command]
    public void CmdFinishTurn()
    {
        NetworkGameManager.Instance.OnPlayerFinishedTurn(playerNumber);
    }

    [Command]
    public void CmdWordGuessed(int wordIndex)
    {
        NetworkGameManager.Instance.OnWordGuessed(playerNumber, wordIndex);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnPlayerAdded?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (isClientOnly || isServer)
        {
            OnPlayerRemoved?.Invoke(this);
        }
    }
    private void OnPlayerNumberChanged(int oldValue, int newValue)
    {
        OnPlayerDataChanged?.Invoke(newValue, playerCountry, isReady, isDanny, role);
    }

    private void OnPlayerCountryChanged(string oldValue, string newValue)
    {
        OnPlayerDataChanged?.Invoke(playerNumber, newValue, isReady, isDanny, role);
    }

    private void OnIsReadyChanged(bool oldValue, bool newValue)
    {
        OnPlayerDataChanged?.Invoke(playerNumber, playerCountry, newValue, isDanny, role);
    }

    private void OnIsDannyChanged(bool oldValue, bool newValue)
    {
        OnPlayerDataChanged?.Invoke(playerNumber, playerCountry, isReady, newValue, role);
    }

    private void OnRoleChanged(Role oldValue, Role newValue)
    {
        OnPlayerDataChanged?.Invoke(playerNumber, playerCountry, isReady, isDanny, newValue);
    }
}