using System.Collections.Generic;
using UnityEngine;

public class PlayingCardsTable : MonoBehaviour
{
    public const float CardsRotationSpeed = 0.5f;
    public const float CardsFlipSpeed = 1000f;
    public const float MinScale = 0.5f;
    public const float MaxScale = 3f;
    public const float ScaleDuration = 0.01f;

    private const float HandCardsDownOffset = 20f;
    private const float HandCardsBetweenOffset = 10f;

    private static List<Card> cardsInHand = new();
    private static List<Card> cardsOnTable = new();
    private static int handCardsLayer;

    public void Start()
    {
        Canvas[] allCanvases = GetComponentsInChildren<Canvas>();
        handCardsLayer = allCanvases.Length;
        foreach (Canvas canv in allCanvases) { 
            Card card = canv.GetComponentInChildren<Card>();
            if (card.InHand) {
                cardsInHand.Add(card);
                card.ChangeLayer(handCardsLayer);
            }
            else {
                cardsOnTable.Add(card);
            }
        }
        ReorderCardsLayers();
        PlaceHandCards();
    }

    public static void PlaceCardFromHandOnTable(Card card)
    {
        cardsInHand.Remove(card);
        cardsOnTable.Add(card);
        card.InHand = false;
        ReorderCardsLayers();
        PlaceHandCards();
    }

    public static void ReturnCardToHand(Card card)
    {
        cardsInHand.Add(card);
        cardsOnTable.Remove(card);
        card.InHand = true;
        card.ChangeLayer(handCardsLayer);
        ReorderCardsLayers();
        PlaceHandCards();
    }

    public static void ChangeCardInOrder(Card card, int offset)
    {
        int index = cardsOnTable.IndexOf(card);
        int nexIndex = index + offset;
        if (nexIndex >= cardsOnTable.Count || nexIndex < 0)
            return;
        cardsOnTable.Remove(card);
        cardsOnTable.Insert(index + offset, card);
        ReorderCardsLayers();
    }

    private static void PlaceHandCards()
    {
        if (cardsInHand.Count == 0) return;
        float width = cardsInHand[0].Width;
        float height = cardsInHand[0].Height;
        float count = cardsInHand.Count;
        float xPos = - (count - 1) * (HandCardsBetweenOffset + width) / 2;
        float yPos = HandCardsDownOffset + (height - Screen.height) / 2;
        foreach (Card card in cardsInHand)
        {
            card.ReturnToHand(new Vector2(xPos, yPos));
            xPos += HandCardsBetweenOffset + width;
        }
    }

    private static void ReorderCardsLayers()
    {
        for (int i = 0; i < cardsOnTable.Count; ++i)
        {
            cardsOnTable[i].ChangeLayer(i);
        }
    }
}
