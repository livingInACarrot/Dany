using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListUI : MonoBehaviour
{
    public static PlayerListUI Instance { get; private set; }

    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private Color dannyColor = Color.red;
    [SerializeField] private Color currentPlayerColor = Color.green;
    [SerializeField] private Color decisivePlayerColor = Color.yellow;
    [SerializeField] private Color localPlayerColor = new(0.9f, 1, 0.9f);

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

    public void RefreshForGame()
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

        GamePlayer gp = GetGamePlayer(player);
        if (gp != null)
        {
            if (gp.IsDanny)
                nameText.color = dannyColor;
            else if (gp.Role == Role.Active)
                nameText.color = currentPlayerColor;
            else if (gp.Role == Role.Decisive)
                nameText.color = decisivePlayerColor;
        }
        else if (player.isLocalPlayer)
        {
            var back = entry.GetComponentInChildren<Image>();
            // На случай, если PlayerEntry будет кнопочкой
            //ColorBlock colors = back.colors;
            //colors.normalColor = localPlayerColor;
            back.color = localPlayerColor;
        }

        Toggle readyToggle = entry.GetComponentInChildren<Toggle>();
        if (readyToggle != null)
        {
            bool inLobby = gp == null;
            readyToggle.gameObject.SetActive(inLobby);
            if (inLobby)
            {
                readyToggle.isOn = player.IsReady;
                readyToggle.interactable = false;

                void OnDataChanged() { if (readyToggle != null) readyToggle.isOn = player.IsReady; }
                player.OnDataChanged += OnDataChanged;
                _readyHandlers[player] = OnDataChanged;
            }
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

    public void HighlightDannyReveal(NetworkPlayer suspectedDanny)
    {
        foreach (var kvp in _entries)
        {
            if (kvp.Key == suspectedDanny)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                if (bg != null) bg.color = dannyColor;
                break;
            }
        }
    }

    private static GamePlayer GetGamePlayer(NetworkPlayer np)
    {
        if (np.GamePlayerNetId == 0) return null;
        if (NetworkClient.spawned.TryGetValue(np.GamePlayerNetId, out NetworkIdentity id))
            return id?.GetComponent<GamePlayer>();
        return null;
    }
}
