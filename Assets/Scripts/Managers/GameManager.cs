using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Основной менеджер игры для оффлайн-режима или синглплеера.
/// Для мультиплеера используется NetworkGameManager.
/// Этот класс может использоваться для тестирования или локальной игры.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int MinPlayers = 1;
    public int MaxPlayers = 8;

    [Header("Debug")]
    public bool DebugMode = false;

    public GamePhase CurrentPhase;
    public List<Player> Players;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<Player> OnTurnChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Устанавливает список игроков
    /// </summary>
    public void SetPlayers(List<Player> players)
    {
        if (players == null || players.Count == 0)
        {
            Debug.LogError("Попытка установить пустой список игроков");
            return;
        }

        Players = players;
        Debug.Log($"Установлено {players.Count} игроков");
    }

    /// <summary>
    /// Добавить игрока в лобби
    /// </summary>
    public void AddPlayer(Player player)
    {
        if (Players == null)
            Players = new List<Player>();
            
        Players.Add(player);
    }

    /// <summary>
    /// Удалить игрока лобби
    /// </summary>
    public void RemovePlayer(int id)
    {
        if (Players == null) return;
        Players.RemoveAll(p => p.Data.Id == id);
    }

    /// <summary>
    /// Создание тестовых игроков для отладки
    /// </summary>
    private void CreateDebugPlayers()
    {
        Players = new List<Player>
        {
            new(new PlayerData("Россия")),
            new(new PlayerData("Грузия")),
            new(new PlayerData("Польша")),
            new(new PlayerData("Венесуэла"))
        };
        PlayerListUI.Instance?.UpdatePlayerList(Players);
        Debug.Log("Созданы тестовые игроки для отладки");
    }

    /// <summary>
    /// Начало игры
    /// </summary>
    public void StartGame()
    {
        if (DebugMode)
        {
            CreateDebugPlayers();
        }
        
        if (Players == null || Players.Count < MinPlayers)
        {
            Debug.LogError($"Недостаточно игроков. Нужно минимум {MinPlayers}, есть {Players?.Count ?? 0}");
            return;
        }

        Debug.Log("Игра начинается!");
        DistributeRoles();
        ChangePhase(GamePhase.RoleDistribution);
        StartNextTurn();
    }

    /// <summary>
    /// Распределение ролей
    /// </summary>
    private void DistributeRoles()
    {
        if (Players == null || Players.Count == 0) return;

        Debug.Log($"Кол-во игроков: {Players.Count}");
        int dannyIndex = UnityEngine.Random.Range(0, Players.Count);
        for (int i = 0; i < Players.Count; i++)
        {
            if (i == dannyIndex)
            {
                Players[i].IsDanny = true;
                Debug.Log($"Дэнни: {Players[i].Data.Id}");
            }
        }
        PlayerListUI.Instance?.UpdatePlayerList(Players);
    }

    /// <summary>
    /// Переход хода к следующему игроку
    /// </summary>
    private void StartNextTurn()
    {
        if (Players == null || Players.Count == 0) return;

        Player currentPlayer = Players.FirstOrDefault(p => p.Role == Role.Active);
        if (currentPlayer == null)
        {
            currentPlayer = Players[UnityEngine.Random.Range(0, Players.Count)];
        }
        else
        {
            int currentIndex = Players.IndexOf(currentPlayer);
            int nextIndex = (currentIndex + 1) % Players.Count;

            Player decisivePlayer = Players.FirstOrDefault(p => p.Role == Role.Decisive);
            if (decisivePlayer != null)
            {
                int ind = Players.IndexOf(decisivePlayer);
                Players[ind].Role = Role.Waiting;
            }
            Players[nextIndex].Role = Role.Active;
            Players[currentIndex].Role = Role.Decisive;
        }

        ChangePhase(GamePhase.TurnInProgress);
        OnTurnChanged?.Invoke(currentPlayer);

        TurnManager.Instance?.StartTurn(currentPlayer);

        PlayerListUI.Instance?.UpdatePlayerList(Players);

        Debug.Log($"Ход игрока: {currentPlayer.Number}");
    }

    /// <summary>
    /// Конец хода
    /// </summary>
    /// <param name="wordGuessedCorrectly"></param>
    public void EndTurn(bool wordGuessedCorrectly)
    {
        if (wordGuessedCorrectly)
        {
            ScoreManager.Instance?.AddPointToPersonalities();
        }
        else
        {
            ScoreManager.Instance?.AddPointToDanny();
        }

        CheckGameEndConditions();

        if (CurrentPhase != GamePhase.GameEnd && CurrentPhase != GamePhase.FinalRound)
        {
            StartNextTurn();
        }
    }

    /// <summary>
    /// Проверка на конец игры
    /// </summary>
    private void CheckGameEndConditions()
    {
        if (ScoreManager.Instance == null) return;

        int personalitiesScore = ScoreManager.Instance.PersonalitiesScore;
        int dannyScore = ScoreManager.Instance.DannyScore;

        if (personalitiesScore >= 6)
        {
            EndGame(false);
        }
        else if (dannyScore >= 3)
        {
            ChangePhase(GamePhase.FinalRound);
            FinalRoundManager.Instance?.StartFinalRound();
        }
        else if (Deck.Instance != null && Deck.Instance.RemainingCards < 7)
        {
            ChangePhase(GamePhase.FinalRound);
            FinalRoundManager.Instance?.StartFinalRound();
        }
    }

    /// <summary>
    /// Конец игры
    /// </summary>
    /// <param name="dannyWins"></param>
    public void EndGame(bool dannyWins)
    {
        ChangePhase(GamePhase.GameEnd);

        if (dannyWins)
        {
            NetworkChat.Instance.AddSystemMessage("Дэнни победил!");
            Debug.Log("Дэнни победил!");
        }
        else
        {
            NetworkChat.Instance.AddSystemMessage("Личности победили!");
            Debug.Log("Личности победили!");
        }

        LobbyManager.Instance?.ShowGameEndScreen(dannyWins);

        Invoke(nameof(ReturnToLobby), 5f);
    }

    /// <summary>
    /// Возврат в лобби
    /// </summary>
    private void ReturnToLobby()
    {
        PlayingCardsTable.Instance?.ClearTable();
        ScoreManager.Instance?.ResetScores();

        if (Players != null)
        {
            foreach (var _ in Players)
            {
                TurnManager.Instance?.ClearHand();
            }
        }

        ChangePhase(GamePhase.Lobby);
        LobbyManager.Instance?.ShowLobby();
    }

    /// <summary>
    /// Смена фазы игры
    /// </summary>
    /// <param name="newPhase"></param>
    public void ChangePhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);

        Debug.Log($"Фаза игры изменена: {newPhase}");
    }

    /// <summary>
    /// Получить игрока по имени
    /// </summary>
    public Player GetPlayerById(int id)
    {
        if (Players == null) return null;

        foreach (var player in Players)
        {
            if (player.Data.Id == id)
                return player;
        }
        return null;
    }

    /// <summary>
    /// Получить всех игроков, кроме указанного
    /// </summary>
    public List<Player> GetOtherPlayers(Player excludePlayer)
    {
        return Players?.Where(p => p != excludePlayer).ToList();
    }
}
