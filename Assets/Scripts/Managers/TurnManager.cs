using Mirror;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SerializeField] private float turnTimeLimit = 60f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartTurn(Player player)
    {
        if (player == null) return;

        bool isActive = (player.Role == Role.Active);

        NetworkChat.Instance.ActivateChat(!isActive);

        if (isActive)
        {
            // Добавить мут всех
            PlayingCardsTable.Instance.ShowHand();
        }
        else
        {
            // Добавить размут всех
        }
        TimerUI.Instance.StartTimer(turnTimeLimit, OnTurnTimeOut);
    }

    private void OnTurnTimeOut()
    {
        OnPlayerFinishedLayingCards();
    }

    public void OnPlayerFinishedLayingCards()
    {
        NetworkChat.Instance.EnableDiscussionMode();

        NetworkPlayer localPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
        if (localPlayer != null && localPlayer.role == Role.Decisive)
        {
            IdeasCardUI.Instance.SetButtonsActive(true);
        }

        // Тута включить голосовой чат для всех
    }
}