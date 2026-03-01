using System;

/// <summary>
/// Перечисление фаз игры
/// </summary>
public enum GamePhase
{
    Lobby,              // Ожидание игроков в лобби
    RoleDistribution,   // Раздача ролей
    TurnInProgress,     // Чей-то ход
    Discussion,         // Обсуждение
    FinalRound,         // Решающий раунд
    GameEnd             // Игра завершена
}
