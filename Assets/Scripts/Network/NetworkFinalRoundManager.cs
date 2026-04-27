using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI и координация финального раунда голосования.
/// Игровая логика разрешения голосов — в NetworkGameManager.ServerResolveVotes.
/// Этот класс только показывает интерфейс и пересылает голоса через CmdVote.
/// Голосование использует лобби-номера игроков (1-based, Loc.Nick).
/// </summary>
public class NetworkFinalRoundManager : NetworkBehaviour
{
    public static NetworkFinalRoundManager Instance { get; private set; }

    [SerializeField] private GameObject finalRoundPanel;
    [SerializeField] private Transform votingButtonsContainer;
    [SerializeField] private GameObject voteButtonPrefab;

    private float discussionTime = 90f;
    private string _currentRoomCode;
    private bool _votingActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // =====================================================
    // Запуск финального раунда
    // Вызывается через RpcStartFinalRound на всех клиентах.
    // dannyLobbyNumber передаётся только для возможного использования на клиенте;
    // итоговое раскрытие идёт через RpcShowVoteResult.
    // =====================================================

    public void StartFinalRound(int dannyLobbyNumber, string roomCode)
    {
        _currentRoomCode = roomCode;
        _votingActive = false;

        finalRoundPanel.SetActive(true);
        BuildVotingButtons(roomCode);
        NetworkChat.Instance?.AddSystemMessage($"Финальный раунд! У вас {discussionTime} секунд на обсуждение.");
        TimerUI.Instance?.StartTimer(discussionTime, OnDiscussionEnd);
    }

    private void BuildVotingButtons(string roomCode)
    {
        foreach (Transform child in votingButtonsContainer)
            Destroy(child.gameObject);

        // GetPlayersNumbers возвращает лобби-номера (1-based)
        List<int> lobbyNumbers = NetworkGameManager.Instance?.GetPlayersNumbers(roomCode) ?? new List<int>();
        foreach (int lobbyNum in lobbyNumbers)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            TMPro.TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Button btn = btnObj.GetComponent<Button>();

            btnText.text = Loc.Nick(lobbyNum);
            btn.interactable = false; // до конца обсуждения

            int captured = lobbyNum;
            btn.onClick.AddListener(() => OnVoteButtonClick(captured, btn));
        }
    }

    private void OnDiscussionEnd()
    {
        _votingActive = true;
        foreach (Transform child in votingButtonsContainer)
            child.GetComponent<Button>().interactable = true;

        NetworkChat.Instance?.AddSystemMessage("Время вышло! Голосуйте за того, кто является Дэнни.");
    }

    public void OnVoteButtonClick(int suspectedLobbyNumber, Button btn)
    {
        if (!_votingActive) return;
        NetworkPlayer localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        localPlayer?.CmdVote(suspectedLobbyNumber);
        btn.interactable = false;
        _votingActive = false;
    }

    // =====================================================
    // ClientRpc — вызываются из NetworkGameManager после разрешения голосов
    // =====================================================

    [ClientRpc]
    public void RpcShowVoteResult(int suspectedLobbyNumber, bool wasDanny)
    {
        string result = wasDanny
            ? $"{Loc.Nick(suspectedLobbyNumber)} — это Дэнни! Личности победили!"
            : $"{Loc.Nick(suspectedLobbyNumber)} — не Дэнни. Дэнни победил!";
        NetworkChat.Instance?.AddSystemMessage(result);
        finalRoundPanel.SetActive(false);
    }

    [ClientRpc]
    public void RpcHandleTie(int dannyLobbyNumber, List<int> tiedLobbyNumbers)
    {
        NetworkChat.Instance?.AddSystemMessage("Ничья в голосовании! Повторное голосование среди равных.");
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
