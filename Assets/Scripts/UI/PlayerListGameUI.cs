using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Список игроков во время игры
/// </summary>
public class PlayerListGameUI : MonoBehaviour
{
    public static PlayerListGameUI Instance { get; private set; }

    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private Color activePlayerColor = new(0.9f, 1f, 0.9f);
    [SerializeField] private Color decisivePlayerColor = new(1f, 1f, 0.9f);
    [SerializeField] private Color waitingPlayerColor = Color.white;

    private readonly Dictionary<GamePlayer, GameObject> _entries = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void UpdatePlayerList(List<GamePlayer> players)
    {
        ClearPlayerList();
        foreach (var player in players)
            if (player != null) CreatePlayerEntry(player);
    }

    public void RefreshList()
    {
        var players = new List<GamePlayer>();
        foreach (var id in NetworkClient.spawned.Values)
        {
            var gp = id?.GetComponent<GamePlayer>();
            if (gp != null) players.Add(gp);
        }
        UpdatePlayerList(players);
    }

    private void CreatePlayerEntry(GamePlayer gp)
    {
        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        _entries[gp] = entry;

        TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
        nameText.text = Loc.Nick(gp.LobbyNumber);

        Image back = entry.GetComponent<Image>();
        if (gp.Role == Role.Active)
            back.color = activePlayerColor;
        else if (gp.Role == Role.Decisive)
            back.color = decisivePlayerColor;
        else
            back.color = waitingPlayerColor;

        Toggle readyToggle = entry.GetComponentInChildren<Toggle>();
        if (readyToggle != null)
            readyToggle.gameObject.SetActive(false);
    }

    private void ClearPlayerList()
    {
        foreach (var kvp in _entries)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        _entries.Clear();
    }
}
