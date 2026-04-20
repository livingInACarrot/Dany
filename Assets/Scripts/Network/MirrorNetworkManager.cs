using Mirror;
using UnityEngine;

/// <summary>
/// Отвечает исключительно за транспортный уровень: подключения/отключения
/// игроков, спавн сетевого экземпляра игрока. Никакой игровой логики здесь нет.
/// Dedicated Server — клиенты подключаются к SERVER_ADDRESS.
/// </summary>
public class MirrorNetworkManager : NetworkManager
{
    public const string SERVER_ADDRESS = "46.138.156.199";

    public override void Awake()
    {
        base.Awake();
        networkAddress = SERVER_ADDRESS;
    }

    // =====================================================
    // Серверные события
    // =====================================================

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

    /// <summary>
    /// Спавним NetworkPlayer для каждого входящего подключения.
    /// playerPrefab задаётся в инспекторе NetworkManager.
    /// </summary>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject playerObj = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, playerObj);
        Debug.Log($"[Server] Player connected: connId={conn.connectionId}");
    }

    /// <summary>
    /// При отключении уведомляем NetworkGameManager, чтобы удалить игрока из комнаты.
    /// </summary>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
            if (player != null)
                NetworkGameManager.Instance?.OnPlayerDisconnected(player);
        }
        base.OnServerDisconnect(conn);
        Debug.Log($"[Server] Player disconnected: connId={conn.connectionId}");
    }

    // =====================================================
    // Клиентские события
    // =====================================================

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[Client] Connecting to {networkAddress}");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        LobbyManager.Instance?.OnDisconnected();
    }
}
