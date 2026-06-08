using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class MirrorNetworkManager : NetworkManager
{
    public const string LocalAddress = "127.0.0.1";
    public const string Server1Address = "146.103.118.171";

    public override void Start()
    {
#if UNITY_SERVER
        StartServer();
        return;
#endif

#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone() &&
            ParrelSync.ClonesManager.GetArgument() == "server")
        {
            StartServer();
            return;
        }
#endif
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"[{DateTime.Now:HH:mm:ss}][Server] Dedicated server started");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log($"[{DateTime.Now:HH:mm:ss}][Server] Stopped");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject playerObj = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, playerObj);
        Debug.Log($"[{DateTime.Now:HH:mm:ss}][Server] Player connected: connId={conn.connectionId}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
            if (player != null)
                NetworkGameManager.Instance.OnPlayerDisconnected(player);
        }
        base.OnServerDisconnect(conn);
        Debug.Log($"[{DateTime.Now:HH:mm:ss}][Server] Player disconnected: connId={conn.connectionId}");
    }

    private bool _wasConnected;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _wasConnected = false;
        Debug.Log($"[{DateTime.Now:HH:mm:ss}][Client] Connecting to {networkAddress}");
        StartCoroutine(DelayedPopup());
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        _wasConnected = true;
        PopupUI.Instance?.Hide();
        Debug.Log($"[{DateTime.Now:HH:mm:ss}][Client] Connected to server");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        if (!_wasConnected)
            PopupUI.Instance?.Show(Loc.Text("popup.server.failedConn"), 4f);
        else
            PopupUI.Instance?.Hide();
        LobbyManager.Instance.OnDisconnected();
    }

    private IEnumerator DelayedPopup()
    {
        yield return new WaitForSeconds(0.5f);
        if (!_wasConnected)
            PopupUI.Instance?.ShowPersistent(Loc.Text("popup.server.conn"));
    }
}
