using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Список игроков в лобби
/// </summary>
public class PlayerListLobbyUI : MonoBehaviour
{
    public static PlayerListLobbyUI Instance { get; private set; }

    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private Color localPlayerColor = new(0.9f, 1f, 0.9f);

    private readonly Dictionary<NetworkPlayer, GameObject> _entries = new();
    private readonly Dictionary<NetworkPlayer, System.Action> _readyHandlers = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void UpdatePlayerList(List<NetworkPlayer> players)
    {
        ClearPlayerList();
        foreach (var player in players)
            if (player != null) CreatePlayerEntry(player);
    }

    public void RefreshList()
    {
        var keys = new List<NetworkPlayer>(_entries.Keys);
        UpdatePlayerList(keys);
    }

    public void RemovePlayer(NetworkPlayer player)
    {
        if (_entries.TryGetValue(player, out GameObject entry))
        {
            if (entry != null) Destroy(entry);
            _entries.Remove(player);
        }
        if (_readyHandlers.TryGetValue(player, out System.Action h))
        {
            player.OnDataChanged -= h;
            _readyHandlers.Remove(player);
        }
    }

    private void CreatePlayerEntry(NetworkPlayer player)
    {
        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        _entries[player] = entry;

        TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
        nameText.text = Loc.Nick(player.Number);

        if (player.isLocalPlayer)
        {
            Image back = entry.GetComponentInChildren<Image>();
            if (back != null) back.color = localPlayerColor;
        }

        Toggle readyToggle = entry.GetComponentInChildren<Toggle>();
        if (readyToggle != null)
        {
            readyToggle.gameObject.SetActive(true);
            readyToggle.isOn = player.IsReady;
            readyToggle.interactable = false;

            void OnDataChanged() { if (readyToggle != null) readyToggle.isOn = player.IsReady; }
            player.OnDataChanged += OnDataChanged;
            _readyHandlers[player] = OnDataChanged;
        }
    }

    private void ClearPlayerList()
    {
        foreach (var kvp in _entries)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
            if (_readyHandlers.TryGetValue(kvp.Key, out System.Action h))
                kvp.Key.OnDataChanged -= h;
        }
        _entries.Clear();
        _readyHandlers.Clear();
    }
}
