using System.Collections.Generic;
using UnityEngine;

public class HandUI : MonoBehaviour
{
    public static HandUI Instance { get; private set; }

    [SerializeField] private Transform handContainer;
    [SerializeField] private float cardSpacing = 10f;
    [SerializeField] private float handYOffset = 50f;

    public RectTransform HandArea { get; private set; }

    private RectTransform handRect;
    private List<Card> currentHand = new List<Card>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        handRect = handContainer.GetComponent<RectTransform>();
        HandArea = handRect;
    }

    public void UpdateHand(List<Card> hand)
    {
        currentHand = hand;
        RepositionCards();
    }

    private void RepositionCards()
    {
        if (currentHand == null || currentHand.Count == 0) return;

        Card firstCard = currentHand[0];
        if (firstCard == null || firstCard.GetComponent<RectTransform>() == null) return;

        float cardWidth = firstCard.Width;
        float totalWidth = (currentHand.Count - 1) * (cardWidth + cardSpacing) + cardWidth;
        float startX = -totalWidth / 2 + cardWidth / 2;
        float yPos = handYOffset;

        for (int i = 0; i < currentHand.Count; i++)
        {
            Card card = currentHand[i];
            if (card == null) continue;

            RectTransform cardRect = card.GetComponent<RectTransform>();

            float xPos = startX + i * (cardWidth + cardSpacing);
            cardRect.SetParent(handContainer);
            cardRect.anchoredPosition = new Vector2(xPos, yPos);

            cardRect.rotation = Quaternion.identity;
            cardRect.localScale = Vector3.one;

        }
    }

    public void ClearHand()
    {
        currentHand.Clear();
    }

    public void AddCardToHand(Card card)
    {
        if (!currentHand.Contains(card))
        {
            currentHand.Add(card);
            RepositionCards();
        }
    }

    public void RemoveCardFromHand(Card card)
    {
        if (currentHand.Contains(card))
        {
            currentHand.Remove(card);
            RepositionCards();
        }
    }
}