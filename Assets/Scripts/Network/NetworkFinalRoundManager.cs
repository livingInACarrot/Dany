using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkFinalRoundManager : NetworkBehaviour
{
    public static NetworkFinalRoundManager Instance { get; private set; }

    [SerializeField] private TMP_Text title;
    [SerializeField] private Transform votingButtonsContainer;
    [SerializeField] private GameObject voteButtonPrefab;
    [SerializeField] private GameObject votePrefab;
    [SerializeField] private float votingTime = 60f;
    [SerializeField] private float resultsTime = 10f;

    private bool _votingActive;
    private string _roomCode;
    private readonly Dictionary<int, Transform> _buttonByLobbyNum = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartFinalRound(int danyLobbyNumber, string roomCode, List<int> lobbyNumbers)
    {
        _roomCode = roomCode;
        LobbyManager.Instance.OnFinalRoundStarted();
        BuildVotingButtons(lobbyNumbers, interactable: true);
        _votingActive = true;
        TimerUI.Instance.StartTimer(votingTime, OnVotingTimerEnded);
        title.text = Loc.Text("gameUI.voteHint");
    }

    public void ShowVoteResults(List<int> suspects, List<int> voterCounts, List<int> votersFlat)
    {
        title.text = Loc.Text("gameUI.voteReveal");

        _votingActive = false;
        foreach (Transform child in votingButtonsContainer)
            child.GetComponent<Button>().interactable = false;

        int voterIdx = 0;
        for (int i = 0; i < suspects.Count; i++)
        {
            int count = voterCounts[i];
            if (_buttonByLobbyNum.TryGetValue(suspects[i], out Transform btnTransform))
            {
                for (int j = 0; j < count; j++)
                {
                    GameObject voteObj = Instantiate(votePrefab, btnTransform.GetComponentInChildren<VerticalLayoutGroup>().transform);
                    voteObj.GetComponentInChildren<TMP_Text>().text = Loc.Nick(votersFlat[voterIdx + j]);
                }
            }
            voterIdx += count;
        }

        TimerUI.Instance.StartTimer(resultsTime, null);
    }

    public void StartTieVote(List<int> tiedLobbyNumbers)
    {
        title.text = Loc.Text("gameUI.voteTie");
        BuildVotingButtons(tiedLobbyNumbers, interactable: true);
        _votingActive = true;
        TimerUI.Instance.StartTimer(votingTime, OnVotingTimerEnded);
    }

    private void BuildVotingButtons(List<int> lobbyNumbers, bool interactable)
    {
        foreach (Transform child in votingButtonsContainer)
            Destroy(child.gameObject);
        _buttonByLobbyNum.Clear();

        GamePlayer localGP = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.GamePlayer;
        int? localNum = localGP?.LobbyNumber;
        foreach (int lobbyNum in lobbyNumbers)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            Button btn = btnObj.GetComponent<Button>();
            btnObj.GetComponentInChildren<TMP_Text>().text = Loc.Nick(lobbyNum);
            btn.interactable = localNum == lobbyNum ? false : interactable;

            int captured = lobbyNum;
            btn.onClick.AddListener(() => OnVoteButtonClick(captured));
            _buttonByLobbyNum[lobbyNum] = btnObj.transform;
        }
    }

    private void OnVoteButtonClick(int suspectedLobbyNumber)
    {
        title.text = Loc.Text("gameUI.voteAccepted");
        if (!_votingActive) return;
        _votingActive = false;
        foreach (Transform child in votingButtonsContainer)
            child.GetComponent<Button>().interactable = false;
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>().CmdVote(suspectedLobbyNumber);
    }

    private void OnVotingTimerEnded()
    {
        _votingActive = false;
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>().CmdVotingTimerEnded(_roomCode);
    }
}
