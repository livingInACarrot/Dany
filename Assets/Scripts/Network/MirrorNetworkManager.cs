using Mirror;
using UnityEngine;

public class MirrorNetworkManager : NetworkManager
{
   // LocalHost
    private readonly string editorAddress = "127.0.0.1";
    //private readonly string editorAddress = "146.103.118.171";
    // Внешний IP 
    private readonly string buildAddress = "46.138.156.199";

    public static string SERVER_ADDRESS = "127.0.0.1";
    //public static string SERVER_ADDRESS = "146.103.118.171";

    public override void Awake()
    {
        base.Awake();

        _ = buildAddress;

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

    private bool _wasConnected;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _wasConnected = false;
        Debug.Log($"[Client] Connecting to {networkAddress}");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        _wasConnected = true;
        Debug.Log("[Client] Connected to server");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        if (!_wasConnected)
            PopupUI.Instance?.Show("Не удалось подключиться к серверу", 4f);
        LobbyManager.Instance.OnDisconnected();
    }
}
