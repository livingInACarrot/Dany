using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NetworkFinalRoundManager : NetworkBehaviour
{
    public static NetworkFinalRoundManager Instance { get; private set; }

    [SerializeField] private GameObject finalRoundPanel;
    [SerializeField] private Transform votingButtonsContainer;
    [SerializeField] private GameObject voteButtonPrefab;

    private Dictionary<int, int> votes = new Dictionary<int, int>(); // voter -> suspect
    private HashSet<int> votedPlayers = new HashSet<int>();
    private bool votingInProgress = false;
    //private float discussionTime = 90f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartFinalRound()
    {
        if (!isServer) return;

        votes.Clear();
        votedPlayers.Clear();
        votingInProgress = true;

        RpcShowFinalRoundUI();

        //Invoke(nameof(StartVoting), discussionTime);
    }

    [ClientRpc]
    private void RpcShowFinalRoundUI()
    {
        finalRoundPanel.SetActive(true);
        CreateVotingButtons();
        NetworkChat.Instance.AddSystemMessage("Решающий раунд! У вас 90 секунд на обсуждение.");
        TimerUI.Instance.StartTimer(90f, OnDiscussionEnd);
    }

    private void CreateVotingButtons()
    {
        foreach (Transform child in votingButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var player in GameManager.Instance.Players)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            Button btn = btnObj.GetComponent<Button>();
            TMPro.TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            btnText.text = $"Игрок {player.Number}";

            int playerNumber = player.Number; // для замыкания
            btn.onClick.AddListener(() => OnVoteButtonClick(playerNumber));

            // Изначально кнопки неактивны
            btn.interactable = false;
        }
    }

    private void OnDiscussionEnd()
    {
        // Включаем кнопки для голосования
        foreach (Transform child in votingButtonsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
                btn.interactable = true;
        }

        NetworkChat.Instance.AddSystemMessage("Время вышло! Голосуйте за того, кого считаете Дэнни.");
    }

    public void OnVoteButtonClick(int suspectedNumber)
    {
        if (!votingInProgress) return;

        NetworkPlayer localPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
        if (localPlayer != null)
        {
            localPlayer.CmdVote(suspectedNumber);
        }

        // Отключаем кнопки после голосования
        foreach (Transform child in votingButtonsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;
        }
    }

    [Server]
    public void OnVoteReceived(int voterNumber, int suspectedNumber)
    {
        if (!votingInProgress) return;

        if (!votedPlayers.Contains(voterNumber))
        {
            votes[voterNumber] = suspectedNumber;
            votedPlayers.Add(voterNumber);

            // Проверяем, все ли проголосовали
            if (votedPlayers.Count >= NetworkGameManager.Instance.playersOrder.Count)
            {
                votingInProgress = false;
                ResolveVotes();
            }
        }
    }

    [Server]
    private void ResolveVotes()
    {
        Dictionary<int, int> voteCounts = new Dictionary<int, int>();

        foreach (int suspect in votes.Values)
        {
            if (!voteCounts.ContainsKey(suspect))
                voteCounts[suspect] = 0;
            voteCounts[suspect]++;
        }

        // Находим игрока с наибольшим количеством голосов
        int maxVotes = 0;
        int mostVoted = -1;

        foreach (var kvp in voteCounts)
        {
            if (kvp.Value > maxVotes)
            {
                maxVotes = kvp.Value;
                mostVoted = kvp.Key;
            }
        }

        // Проверяем, есть ли явный победитель
        if (mostVoted == -1 || maxVotes <= votes.Count / 2)
        {
            // Ничья или нет большинства
            HandleTie();
            return;
        }

        // Проверяем, является ли подозреваемый Дэнни
        bool isDanny = (mostVoted == NetworkGameManager.Instance.dannyPlayerNumber);

        if (isDanny)
        {
            RpcShowVoteResult(mostVoted, true);
            NetworkGameManager.Instance.EndGame(false); // Дэнни проиграл
        }
        else
        {
            RpcShowVoteResult(mostVoted, false);
            NetworkGameManager.Instance.EndGame(true); // Дэнни выиграл
        }
    }

    [Server]
    private void HandleTie()
    {
        RpcShowTieMessage();

        // Находим претендентов (те, кто получил максимальное число голосов)
        Dictionary<int, int> voteCounts = new Dictionary<int, int>();
        foreach (int suspect in votes.Values)
        {
            if (!voteCounts.ContainsKey(suspect))
                voteCounts[suspect] = 0;
            voteCounts[suspect]++;
        }

        int maxVotes = voteCounts.Values.Max();
        List<int> tiedPlayers = voteCounts.Where(kvp => kvp.Value == maxVotes)
                                          .Select(kvp => kvp.Key)
                                          .ToList();

        // Проверяем, есть ли Дэнни среди раскрывшихся
        bool dannyFound = false;
        foreach (int playerNum in NetworkGameManager.Instance.playersOrder)
        {
            if (!tiedPlayers.Contains(playerNum))
            {
                if (playerNum == NetworkGameManager.Instance.dannyPlayerNumber)
                {
                    dannyFound = true;
                    RpcDannyRevealed(playerNum);
                    NetworkGameManager.Instance.EndGame(true);
                    return;
                }
            }
        }

        if (!dannyFound)
        {
            RpcSecondRoundVoting(tiedPlayers);
            StartSecondRound(tiedPlayers);
        }
    }

    [ClientRpc]
    private void RpcShowVoteResult(int suspectedNumber, bool isDanny)
    {
        string result = isDanny
            ? $"Игрок {suspectedNumber} - Дэнни! Личности побеждают!"
            : $"Игрок {suspectedNumber} не Дэнни. Дэнни побеждает!";

        NetworkChat.Instance.AddSystemMessage(result);
        finalRoundPanel.SetActive(false);
    }

    [ClientRpc]
    private void RpcShowTieMessage()
    {
        NetworkChat.Instance.AddSystemMessage("Ничья в голосовании! Претенденты не раскрываются.");
    }

    [ClientRpc]
    private void RpcDannyRevealed(int dannyNumber)
    {
        NetworkChat.Instance.AddSystemMessage($"Игрок {dannyNumber} был Дэнни! Дэнни побеждает.");
        finalRoundPanel.SetActive(false);
    }

    [ClientRpc]
    private void RpcSecondRoundVoting(List<int> tiedPlayers)
    {
        NetworkChat.Instance.AddSystemMessage("Среди раскрывшихся нет Дэнни! Голосуйте снова среди претендентов.");

        foreach (Transform child in votingButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (int playerNum in tiedPlayers)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            Button btn = btnObj.GetComponent<Button>();
            TMPro.TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            btnText.text = $"Игрок {playerNum}";

            int number = playerNum;
            btn.onClick.AddListener(() => OnSecondRoundVote(number));
            btn.interactable = true;
        }
    }

    [Server]
    private void StartSecondRound(List<int> tiedPlayers)
    {
        votes.Clear();
        votedPlayers.Clear();
        votingInProgress = true;
    }

    public void OnSecondRoundVote(int suspectedNumber)
    {
        NetworkPlayer localPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
        if (localPlayer != null)
        {
            localPlayer.CmdVote(suspectedNumber);
        }
    }
}