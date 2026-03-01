using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerListUI : MonoBehaviour
{
    public static PlayerListUI Instance { get; private set; }

    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private Color dannyColor = Color.red;
    [SerializeField] private Color currentPlayerColor = Color.green;
    [SerializeField] private Color decisivePlayerColor = Color.yellow;

    private Dictionary<Player, GameObject> playerEntries = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void UpdatePlayerList(List<Player> players)
    {
        ClearPlayerList();

        foreach (var player in players)
        {
            if (player != null)
            {
                CreatePlayerEntry(player);
            }
        }
    }

    private void CreatePlayerEntry(Player player)
    {
        if (player == null)
            return;
        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        playerEntries[player] = entry;

        TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();

        string playerName = LocalizationManager.Instance.GetText("voice");

        nameText.text = $"{playerName} {player.Number} ({player.Data.Country})";

        if (player.Role == Role.Active)
        {
            nameText.color = currentPlayerColor;
        }
        else if (player.Role == Role.Decisive)
        {
            nameText.color = decisivePlayerColor;
        }

        if (player.IsDanny)
        {
            //nameText.text += " (Дэнни)";
            nameText.color = dannyColor;
        }

        Toggle readyToggle = entry.GetComponentInChildren<Toggle>();
        readyToggle.gameObject.SetActive(true);
        readyToggle.isOn = player.IsReady;
        readyToggle.interactable = false;
    }

    private void ClearPlayerList()
    {
        foreach (var entry in playerEntries.Values)
        {
            if (entry != null)
                Destroy(entry);
        }
        playerEntries.Clear();
    }

    public void HighlightDannyReveal(Player suspectedDanny)
    {
        foreach (var kvp in playerEntries)
        {
            if (kvp.Key == suspectedDanny)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                if (bg != null)
                    bg.color = dannyColor;
                break;
            }
        }
    }
}