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

    private Dictionary<NetworkPlayer, GameObject> playerEntries = new();

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

    /// <summary>
    /// Перерисовать список с учётом игровых данных из GamePlayer
    /// (роли, IsDanny). Вызывается после спавна GamePlayer.
    /// </summary>
    public void RefreshForGame()
    {
        var keys = new List<NetworkPlayer>(playerEntries.Keys);
        UpdatePlayerList(keys);
    }

    private void CreatePlayerEntry(NetworkPlayer player)
    {
        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        playerEntries[player] = entry;

        TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
        nameText.text = Loc.Nick(player.Number);

        // Пытаемся получить игровые данные из GamePlayer
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

        Toggle readyToggle = entry.GetComponentInChildren<Toggle>();
        if (readyToggle != null)
        {
            // Готовность показываем только в фазе лобби
            bool inLobby = gp == null;
            readyToggle.gameObject.SetActive(inLobby);
            if (inLobby)
            {
                readyToggle.isOn = player.IsReady;
                readyToggle.interactable = false;
            }
        }
    }

    private void ClearPlayerList()
    {
        foreach (var entry in playerEntries.Values)
            if (entry != null) Destroy(entry);
        playerEntries.Clear();
    }

    public void HighlightDannyReveal(NetworkPlayer suspectedDanny)
    {
        foreach (var kvp in playerEntries)
        {
            if (kvp.Key == suspectedDanny)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                if (bg != null) bg.color = dannyColor;
                break;
            }
        }
    }

    /// <summary>Найти GamePlayer по GamePlayerNetId из NetworkPlayer.</summary>
    private static GamePlayer GetGamePlayer(NetworkPlayer np)
    {
        if (np.GamePlayerNetId == 0) return null;
        if (NetworkClient.spawned.TryGetValue(np.GamePlayerNetId, out NetworkIdentity id))
            return id?.GetComponent<GamePlayer>();
        return null;
    }
}
