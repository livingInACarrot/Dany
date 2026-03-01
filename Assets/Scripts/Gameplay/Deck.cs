using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public static Deck Instance { get; private set; }

    [SerializeField] private Transform tableCanvasTransform;
    [SerializeField] private GameObject cardPrefab;

    private Queue<Sprite> pictureCards = new();
    private List<Card> discardedCards = new();

    public int RemainingCards => pictureCards.Count;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializeDeck();
    }

    private void InitializeDeck()
    {
        List<Sprite> shuffled = new(CardsStorage.PictureCardsSprites);
        Shuffle(shuffled);
        Debug.Log($"pic cards amount = {CardsStorage.PictureCardsSprites.Count}");
        pictureCards.Clear();
        foreach (var sprite in shuffled)
        {
            pictureCards.Enqueue(sprite);
        }
    }

    public Card DrawCard()
    {
        if (pictureCards.Count == 0)
        {
            Debug.LogError("Нет карт в колоде!");
            return null;
        }

        Sprite cardSprite = pictureCards.Dequeue();
        GameObject cardObj = Instantiate(cardPrefab, tableCanvasTransform);
        Card card = cardObj.GetComponent<Card>();
        card.SetSprite(cardSprite);
        card.InHand = true;
        return card;
    }

    public void DrawCardsToHand()
    {
        for (int i = 0; i < 7; i++)
        {
            TurnManager.Instance.handCards.Add(DrawCard());
        }
        HandUI.Instance.UpdateHand(TurnManager.Instance.handCards);
    }

    public void DiscardCard(Card card)
    {
        discardedCards.Add(card);
        Destroy(card.gameObject);
    }

    private void Shuffle<Card>(List<Card> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[j], list[i]) = (list[i], list[j]);
        }
    }
}