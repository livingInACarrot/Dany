using System.IO;
using Mirror;
using UnityEngine;

public class MirrorNetworkManager : NetworkManager
{
    private readonly string editorAddress = "127.0.0.1";
    private readonly string fallbackAddress = "46.138.156.199";

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
        networkAddress = LoadServerAddress();
#endif
    }

    private string LoadServerAddress()
    {
        string configPath = Path.Combine(Application.dataPath, "../server_config.txt");
        if (File.Exists(configPath))
        {
            string address = File.ReadAllText(configPath).Trim();
            if (!string.IsNullOrEmpty(address))
            {
                Debug.Log($"[Client] Server address loaded from config: {address}");
                return address;
            }
        }
        Debug.Log($"[Client] Config not found, using fallback address: {fallbackAddress}");
        return fallbackAddress;
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
        PopupUI.Instance?.ShowPersistent("Подключение к серверу...");
        Debug.Log($"[Client] Connecting to {networkAddress}");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        _wasConnected = true;
        PopupUI.Instance?.Hide();
        Debug.Log("[Client] Connected to server");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        if (!_wasConnected)
            PopupUI.Instance?.Show("Не удалось подключиться к серверу", 4f);
        else
            PopupUI.Instance?.Hide();
        LobbyManager.Instance.OnDisconnected();
    }
}
