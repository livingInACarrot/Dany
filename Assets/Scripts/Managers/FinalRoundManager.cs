using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Сделать правильный вывод
public class FinalRoundManager : MonoBehaviour
{
    public static FinalRoundManager Instance { get; private set; }

    [SerializeField] private GameObject finalRoundPanel;
    [SerializeField] private Transform votingButtonsContainer;
    [SerializeField] private GameObject voteButtonPrefab;
    [SerializeField] private float discussionTime = 90f;

    private Dictionary<Player, Button> voteButtons = new();
    private Dictionary<Player, int> votes = new();
    private bool hasVoted = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        finalRoundPanel.SetActive(false);
    }

    public void StartFinalRound()
    {
        finalRoundPanel.SetActive(true);
        hasVoted = false;
        votes.Clear();

        CreateVotingButtons();

        NetworkChat.Instance.AddSystemMessage($"Решающий раунд! У вас {discussionTime} на обсуждение, затем голосование.");
        TimerUI.Instance.StartTimer(discussionTime, OnDiscussionEnd);
    }

    private void CreateVotingButtons()
    {
        foreach (Transform child in votingButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        voteButtons.Clear();

        foreach (var player in GameManager.Instance.Players)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            Button btn = btnObj.GetComponent<Button>();

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            // Добавить сюда "Голос номер.." с ссылкой на таблицу
            btnText.text = "" ;

            btn.onClick.AddListener(() => OnVote(player));
            btn.interactable = false;
            voteButtons[player] = btn;
        }
    }

    private void OnDiscussionEnd()
    {
        foreach (var btn in voteButtons.Values)
        {
            btn.interactable = true;
        }
        NetworkChat.Instance.AddSystemMessage("Время вышло! Голосуйте за того, кого считаете Дэнни.");
    }

    public void OnVote(Player suspectedPlayer)
    {
        if (hasVoted) return;

        hasVoted = true;
        foreach (var btn in voteButtons.Values)
        {
            btn.interactable = false;
        }

        if (!votes.ContainsKey(suspectedPlayer))
            votes[suspectedPlayer] = 0;
        votes[suspectedPlayer]++;

        StartCoroutine(WaitForAllVotes());
    }

    private IEnumerator WaitForAllVotes()
    {
        yield return new WaitForSeconds(2f);
        ResolveVoting();
    }

    private void ResolveVoting()
    {
        Player mostVoted = null;
        int maxVotes = 0;

        foreach (var kvp in votes)
        {
            if (kvp.Value > maxVotes)
            {
                maxVotes = kvp.Value;
                mostVoted = kvp.Key;
            }
        }

        if (mostVoted == null)
        {
            ResolveTie();
            return;
        }

        bool isDanny = (mostVoted == GameManager.Instance.Players.FirstOrDefault(p => p.IsDanny));
        PlayerListUI.Instance.HighlightDannyReveal(mostVoted);

        if (isDanny)
        {
            NetworkChat.Instance.AddSystemMessage($"Победа! {mostVoted.Data.Id} - Дэнни! Личности побеждают.");
            GameManager.Instance.EndGame(false);
        }
        else
        {
            NetworkChat.Instance.AddSystemMessage($"Поражение! {mostVoted.Data.Id} не Дэнни. Дэнни побеждает.");
            GameManager.Instance.EndGame(true);
        }

        finalRoundPanel.SetActive(false);
    }

    private void ResolveTie()
    {
        NetworkChat.Instance.AddSystemMessage("Ничья в голосовании! Претенденты не раскрываются.");

        // Находим претендентов с максимальным числом голосов
        List<Player> tiedPlayers = new();
        int maxVotes = 0;

        foreach (var kvp in votes)
        {
            if (kvp.Value > maxVotes)
            {
                maxVotes = kvp.Value;
                tiedPlayers.Clear();
                tiedPlayers.Add(kvp.Key);
            }
            else if (kvp.Value == maxVotes)
            {
                tiedPlayers.Add(kvp.Key);
            }
        }

        bool dannyFound = false;
        foreach (var player in GameManager.Instance.Players)
        {
            if (!tiedPlayers.Contains(player))
            {
                if (player.IsDanny)
                {
                    dannyFound = true;
                    NetworkChat.Instance.AddSystemMessage($"{player.Data.Id} был Дэнни! Дэнни побеждает.");
                    GameManager.Instance.EndGame(true);
                    return;
                }
            }
        }

        if (!dannyFound)
        {
            // Среди раскрывшихся нет Дэнни - повторное голосование
            NetworkChat.Instance.AddSystemMessage("Среди раскрывшихся нет Дэнни! Голосуйте снова.");

            // Создаем новые кнопки только для претендентов
            foreach (Transform child in votingButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var player in tiedPlayers)
            {
                GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
                Button btn = btnObj.GetComponent<Button>();
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                btnText.text = player.Data.Id.ToString();
                btn.onClick.AddListener(() => OnSecondRoundVote(player));
                btn.interactable = true;
            }
            hasVoted = false;
        }
    }

    private void OnSecondRoundVote(Player suspectedPlayer)
    {
        if (hasVoted) return;

        hasVoted = true;

        if (suspectedPlayer.IsDanny)
        {
            NetworkChat.Instance.AddSystemMessage($"{suspectedPlayer.Data.Id} - Дэнни! Личности побеждают.");
            GameManager.Instance.EndGame(false);
        }
        else
        {
            NetworkChat.Instance.AddSystemMessage($"{suspectedPlayer.Data.Id} не Дэнни. Дэнни побеждает.");
            GameManager.Instance.EndGame(true);
        }
        finalRoundPanel.SetActive(false);
    }
}
