using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartTurnTimer(PlayerData player)
    {
        TimerUI.Instance.StartTimer(60f, () => OnTurnTimeOut(player));
    }

    private void OnTurnTimeOut(PlayerData player)
    {
        Debug.Log($"Время для хода вышло");
        // Не забыть добавить логику обработки таймаута
    }
}