using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Mirror;

/// <summary>
/// Логгер для измерения различных метрик сетевого соединения (задержки, нагрузки и т.п.)
/// </summary>
public class NetworkLogger : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Как часто писать строку в лог, секунд")]
    public float sampleInterval = 1f;

    [Tooltip("Логировать на сервере")]
    public bool logOnServer = true;

    [Tooltip("Логировать на клиенте")]
    public bool logOnClient = true;

    private static int _commandsThisSecond;
    private static int _rpcsThisSecond;
    private static long _bytesSentThisSecond;
    private float _maxTickMs;

    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private string _filePath;
    private string _stopSignalPath;
    private float _timer;
    private bool _paused;
    private StringBuilder _sb = new();

    void Start()
    {
        bool isDedicatedServer = NetworkServer.active && !NetworkClient.active;
        if (isDedicatedServer && !logOnServer) { enabled = false; return; }
        if (!isDedicatedServer && !logOnClient) { enabled = false; return; }

        string role = isDedicatedServer ? "server" : "client";
        string fileName = $"netlog_{role}_{DateTime.Now:dd.MM.yyyy_HH.mm}.csv";
        _filePath = Path.Combine(Application.persistentDataPath, fileName);

        bool isServer = NetworkServer.active && !NetworkClient.active;

        string header = "sep=;\nSeconds;Role;Conn;Bytes;Memory(Mb);";
        header += isServer ? "Tick(ms);Max tick(ms);Cmd/sec.;Rpc/sec.\n" : "RTT(ms);fps\n";

        File.WriteAllText(_filePath, header, Utf8Bom);

        _stopSignalPath = Path.Combine(Application.persistentDataPath, "netlog.stop");

        NetworkDiagnostics.OutMessageEvent += OnOutMessage;
        NetworkDiagnostics.InMessageEvent  += OnInMessage;

        Debug.Log($"[NetworkLogger] Пишу метрики в: {_filePath}");
        Debug.Log($"[NetworkLogger] Пауза/возобновление: touch/rm {_stopSignalPath}");
    }

    void OnDestroy()
    {
        NetworkDiagnostics.OutMessageEvent -= OnOutMessage;
        NetworkDiagnostics.InMessageEvent -= OnInMessage;
    }

    private void OnOutMessage(NetworkDiagnostics.MessageInfo info)
    {
        if (info.message is RpcMessage)
            _rpcsThisSecond += info.count;
        _bytesSentThisSecond += info.bytes * info.count;
    }

    private void OnInMessage(NetworkDiagnostics.MessageInfo info)
    {
        if (info.message is CommandMessage)
            _commandsThisSecond += info.count;
    }

    void Update()
    {
        float tickMs = Time.unscaledDeltaTime * 1000f;
        if (tickMs > _maxTickMs) _maxTickMs = tickMs;

        _timer += Time.unscaledDeltaTime;
        if (_timer < sampleInterval) return;
        _timer = 0f;

        bool stopFileExists = File.Exists(_stopSignalPath);
        if (stopFileExists && !_paused)
        {
            _paused = true;
            Debug.Log("[NetworkLogger] Запись приостановлена.");
        }
        else if (!stopFileExists && _paused)
        {
            _paused = false;
            Debug.Log("[NetworkLogger] Запись возобновлена.");
        }
        if (_paused) return;

        bool isDedicatedServer = NetworkServer.active && !NetworkClient.active;
        string role = isDedicatedServer ? "server" : "client";

        int connections = isDedicatedServer ? NetworkServer.connections.Count : (NetworkClient.isConnected ? 1 : 0);

        float memMb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

        var ru = new CultureInfo("ru-RU");
        _sb.Clear();
        _sb.Append(Time.realtimeSinceStartup.ToString("F0", ru)).Append(';')
           .Append(role).Append(';')
           .Append(connections).Append(';')
            .Append(_bytesSentThisSecond).Append(';')
           .Append(memMb.ToString("F1", ru)).Append(';');

        if (isDedicatedServer)
        {
            _sb.Append(tickMs.ToString("F2", ru)).Append(';')
               .Append(_maxTickMs.ToString("F2", ru)).Append(';')
               .Append(_commandsThisSecond).Append(';')
               .Append(_rpcsThisSecond).Append(';').Append('\n');    
        }
        else
        {
            _sb.Append((NetworkTime.rtt * 1000.0).ToString("F1", ru)).Append(';')
               .Append(fps.ToString("F0", ru)).Append('\n');
        }

        File.AppendAllText(_filePath, _sb.ToString(), Utf8Bom);

        _commandsThisSecond = 0;
        _rpcsThisSecond = 0;
        _bytesSentThisSecond = 0;
        _maxTickMs = 0f;
    }
}