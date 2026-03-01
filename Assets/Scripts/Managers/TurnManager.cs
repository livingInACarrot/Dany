using Mirror.Examples.Chat;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

// Добавить тоже имена игроков

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SerializeField] private float turnTimeLimit = 60f;
    public List<Card> handCards;

    private IdeasCard currentIdeasCard;
    private int secretWordIndex;
    [SerializeField] private GameObject hintText;
    [SerializeField] private GameObject hand;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartTurn(Player player)
    {
        currentIdeasCard = IdeasDeck.Instance.DrawCard();

        NetworkChat.Instance.ActivateChat(true);
        if (player == LobbyManager.Instance.localPlayer)
        {
            // Если мы ходим
            NetworkChat.Instance.ActivateChat(false);
            ShowActiveRoleView(currentIdeasCard, currentIdeasCard.GetRandomWord());
        }
        else
        {
            ShowOthers(currentIdeasCard);
        }

        ClearHand();
        Deck.Instance.DrawCardsToHand();

        TimerUI.Instance.StartTimer(turnTimeLimit, OnTurnTimeOut);

        IdeasCardUI.Instance.SetButtonsActive(false);
    }

    private void OnTurnTimeOut()
    {
        OnPlayerFinishedLayingCards();
        //GameManager.Instance.EndTurn(false);
        //bool guessedCorrectly = false;
        /*
        if (currentTurnPlayer.Id != GameManager.Instance.DannyPlayer.Id)
        {
            GameManager.Instance.EndTurn(false);
        }
        else
        {
            // Дэнни не успел - это подозрительно, но формально слово не угадали
            // В реальной игре нужно голосование, но для MVP - очко Дэнни
            GameManager.Instance.EndTurn(false);
        }
        */
    }

    public void OnPlayerFinishedLayingCards()
    {
        hintText.SetActive(true);
        hand.SetActive(false);

        GameManager.Instance.ChangePhase(GamePhase.Discussion);

        NetworkChat.Instance.EnableDiscussionMode();

        if (LobbyManager.Instance.localPlayer.Role == Role.Decisive)
        {
            IdeasCardUI.Instance.SetButtonsActive(true);
        }

        TimerUI.Instance.StartTimer(30f, OnDiscussionTimeOut);
    }
    public void ClearHand()
    {
        handCards.Clear();
    }

    private void OnDiscussionTimeOut()
    {
        IdeasCardUI.Instance.ShowGuessPanel(currentIdeasCard);
    }

    public void OnWordGuessed(int guessedWordIndex)
    {
        bool isCorrect = (guessedWordIndex == secretWordIndex);

        Debug.Log($"Загадано: {secretWordIndex}, Угадано: {guessedWordIndex} - {(isCorrect ? "Верно" : "Неверно")}");

        GameManager.Instance.EndTurn(isCorrect);
    }

    private void ShowActiveRoleView(IdeasCard card, int word)
    {
        IdeasCardUI.Instance.ShowForActiveRole(card, word);
    }

    private void ShowOthers(IdeasCard card)
    {
        IdeasCardUI.Instance.ShowForOthers(card);
    }
}