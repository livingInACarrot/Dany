using Mirror;
using UnityEngine;

/// <summary>
/// Отвечает соединение игроков
/// </summary>
public class MirrorNetworkManager : NetworkManager
{
   // LocalHost
    private readonly string editorAddress = "127.0.0.1";
    // Внешний IP 
    private readonly string buildAddress = "46.138.156.199";

    public static string SERVER_ADDRESS = "127.0.0.1";

    public override void Awake()
    {
        base.Awake();

#if UNITY_SERVER
        return;
#endif

#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone() && ParrelSync.ClonesManager.GetArgument() == "server")
            return;
        networkAddress = editorAddress;
#else
        networkAddress = buildAddress;
#endif
    }

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

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[Client] Connected to server");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        LobbyManager.Instance.OnDisconnected();
    }
}
