using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI и координация финального раунда голосования.
/// Игровая логика разрешения голосов — в NetworkGameManager.ServerResolveVotes.
/// Этот класс только показывает интерфейс и пересылает голоса через CmdVote.
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
    // Запуск финального раунда (вызывается через RpcStartFinalRound)
    // =====================================================

    /// <summary>
    /// dannyIndex — RoomIndex Дэнни (нужен только для подсветки кнопок, логика на сервере).
    /// roomCode — для маршрутизации голосов.
    /// </summary>
    public void StartFinalRound(int dannyIndex, string roomCode)
    {
        if (!isServer) return;
        _currentRoomCode = roomCode;
        _votingActive = true;
        RpcShowFinalRoundUI(roomCode);
    }

    [ClientRpc]
    private void RpcShowFinalRoundUI(string roomCode)
    {
        _currentRoomCode = roomCode;
        _votingActive = false; // кнопки неактивны до конца обсуждения

        finalRoundPanel.SetActive(true);
        BuildVotingButtons(roomCode);
        NetworkChat.Instance?.AddSystemMessage($"Финальный раунд! У вас {discussionTime} секунд на обсуждение.");
        TimerUI.Instance?.StartTimer(discussionTime, OnDiscussionEnd);
    }

    private void BuildVotingButtons(string roomCode)
    {
        foreach (Transform child in votingButtonsContainer)
            Destroy(child.gameObject);

        List<int> indices = NetworkGameManager.Instance?.GetPlayersNumbers(roomCode) ?? new List<int>();
        foreach (int idx in indices)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            TMPro.TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Button btn = btnObj.GetComponent<Button>();

            btnText.text = Loc.Nick(idx);
            btn.interactable = false; // до конца обсуждения

            int captured = idx;
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

    public void OnVoteButtonClick(int suspectedRoomIndex, Button btn)
    {
        if (!_votingActive) return;
        NetworkPlayer localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        localPlayer?.CmdVote(suspectedRoomIndex);
        btn.interactable = false;
        _votingActive = false; // один голос
    }

    // =====================================================
    // ClientRpc — вызываются из NetworkGameManager после разрешения голосов
    // =====================================================

    [ClientRpc]
    public void RpcShowVoteResult(int suspectedIndex, bool wasDanny)
    {
        string result = wasDanny
            ? $"Игрок {Loc.Nick(suspectedIndex)} — это Дэнни! Личности победили!"
            : $"Игрок {Loc.Nick(suspectedIndex)} — не Дэнни. Дэнни победил!";
        NetworkChat.Instance?.AddSystemMessage(result);
        finalRoundPanel.SetActive(false);
    }

    [ClientRpc]
    public void RpcHandleTie(int dannyIndex, List<int> tiedPlayers)
    {
        NetworkChat.Instance?.AddSystemMessage("Ничья в голосовании! Повторное голосование среди равных.");
        RebuildButtonsForTie(tiedPlayers);
        _votingActive = true;
    }

    private void RebuildButtonsForTie(List<int> tiedPlayers)
    {
        foreach (Transform child in votingButtonsContainer)
            Destroy(child.gameObject);

        foreach (int idx in tiedPlayers)
        {
            GameObject btnObj = Instantiate(voteButtonPrefab, votingButtonsContainer);
            TMPro.TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Button btn = btnObj.GetComponent<Button>();

            btnText.text = Loc.Nick(idx);
            btn.interactable = true;

            int captured = idx;
            btn.onClick.AddListener(() => OnVoteButtonClick(captured, btn));
        }
    }
}
