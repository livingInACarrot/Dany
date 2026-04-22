using Mirror;
using UnityEngine;

/// <summary>
/// Отвечает исключительно за транспортный уровень
/// </summary>
public class MirrorNetworkManager : NetworkManager
{
    //public const string SERVER_ADDRESS = "46.138.156.199";
    public const string SERVER_ADDRESS = "localhost";

    public override void Awake()
    {
        base.Awake();
        networkAddress = SERVER_ADDRESS;
    }

    public override void Start()
    {
#if UNITY_SERVER
        StartServer();
#elif UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone() &&
            ParrelSync.ClonesManager.GetArgument() == "server")
        {
            networkAddress = "localhost";
            StartServer();
        }
#endif
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[Server] Dedicated server started");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("[Server] Stopped");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject playerObj = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, playerObj);
        Debug.Log($"[Server] Player connected: connId={conn.connectionId}");
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
        Debug.Log($"[Server] Player disconnected: connId={conn.connectionId}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[Client] Connecting to {networkAddress}");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        LobbyManager.Instance.OnDisconnected();
    }
}
