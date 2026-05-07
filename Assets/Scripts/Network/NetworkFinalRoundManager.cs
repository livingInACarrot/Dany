using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class NetworkFinalRoundManager : NetworkBehaviour
{
    public static NetworkFinalRoundManager Instance { get; private set; }

    [SerializeField] private Transform votingButtonsContainer;
    [SerializeField] private GameObject voteButtonPrefab;

    private readonly float discussionTime = 60f;
    private bool _votingActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartFinalRound(int danyLobbyNumber, string roomCode, List<int> lobbyNumbers)
    {
        _votingActive = false;

        LobbyManager.Instance.OnFinalRoundStarted();
        BuildVotingButtons(lobbyNumbers);
        ChatUI.Instance.AddSystemMessage($"Финальный раунд! У вас {discussionTime} секунд на обсуждение.");
        TimerUI.Instance.StartTimer(discussionTime, OnDiscussionEnd);
    }

    private void BuildVotingButtons(List<int> lobbyNumbers)
    {
        foreach (Transform child in votingButtonsContainer)
            Destroy(child.gameObject);

        foreach (int lobbyNum in lobbyNumbers)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            TMPro.TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Button btn = btnObj.GetComponent<Button>();

            btnText.text = Loc.Nick(lobbyNum);
            btn.interactable = false;

            int captured = lobbyNum;
            btn.onClick.AddListener(() => OnVoteButtonClick(captured, btn));
        }
    }

    private void OnDiscussionEnd()
    {
        _votingActive = true;
        foreach (Transform child in votingButtonsContainer)
            child.GetComponent<Button>().interactable = true;

        ChatUI.Instance.AddSystemMessage("Время вышло! Голосуйте за того, кто является Дэни.");
    }

    public void OnVoteButtonClick(int suspectedLobbyNumber, Button btn)
    {
        if (!_votingActive) return;
        NetworkPlayer localPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
        localPlayer.CmdVote(suspectedLobbyNumber);
        btn.interactable = false;
        _votingActive = false;
    }

    [ClientRpc]
    public void RpcShowVoteResult(int suspectedLobbyNumber, bool wasDany)
    {
        string result = wasDany
            ? $"{Loc.Nick(suspectedLobbyNumber)} — это Дэни! Личности победили!"
            : $"{Loc.Nick(suspectedLobbyNumber)} — не Дэни. Дэни победил!";
        ChatUI.Instance.AddSystemMessage(result);
        LobbyManager.Instance.HideFinalRoundPanel();
    }

    [ClientRpc]
    public void RpcHandleTie(int danyLobbyNumber, List<int> tiedLobbyNumbers)
    {
        ChatUI.Instance.AddSystemMessage("Ничья в голосовании! Повторное голосование среди равных.");
        RebuildButtonsForTie(tiedLobbyNumbers);
        _votingActive = true;
    }

    private void RebuildButtonsForTie(List<int> tiedLobbyNumbers)
    {
        foreach (Transform child in votingButtonsContainer)
            Destroy(child.gameObject);

        foreach (int lobbyNum in tiedLobbyNumbers)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            TMPro.TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Button btn = btnObj.GetComponent<Button>();

            btnText.text = Loc.Nick(lobbyNum);
            btn.interactable = true;

            int captured = lobbyNum;
            btn.onClick.AddListener(() => OnVoteButtonClick(captured, btn));
        }
    }
}
