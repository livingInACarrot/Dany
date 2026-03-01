using System.Collections.Generic;
using UnityEngine;

public class PlayingCardsTable : MonoBehaviour
{
    public static PlayingCardsTable Instance { get; private set; }

    [SerializeField] private RectTransform tableArea;
    [SerializeField] private RectTransform handArea;

    private void Awake()
    {
        Instance = this;
    }

    public void PlaceCardFromHandOnTable(Card card)
    {
        card.InHand = false;
        card.transform.SetParent(Instance.tableArea);
    }

    public void ReturnCardToHand(Card card)
    {
        card.ReturnToHand();
        card.transform.SetParent(Instance.handArea);
    }

    public bool IsOverTableArea(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            tableArea,
            screenPosition,
            null);
    }

    public bool IsOverHandArea(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            handArea,
            screenPosition,
            null);
    }

    public void ClearTable()
    {
        Card[] cardsOnTable = tableArea.GetComponentsInChildren<Card>();
        foreach (Card card in cardsOnTable) 
        {
            card.ReturnToHand();
            card.transform.SetParent(handArea);
        }
    }
    public void HideHand()
    {
        handArea.gameObject.SetActive(false);
    }
    public void ShowHand()
    {
        handArea.gameObject.SetActive(true);
    }
}