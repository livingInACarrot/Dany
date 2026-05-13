public enum GamePhase
{
    Lobby,              // Ожидание игроков в лобби
    RoleDistribution,   // Раздача ролей
    TurnInProgress,     // Чей-то ход
    Discussion,         // Обсуждение
    WordReveal,         // Показывается загаданное слово
    FinalRound,         // Решающий раунд
    GameEnd             // Игра завершена
}
