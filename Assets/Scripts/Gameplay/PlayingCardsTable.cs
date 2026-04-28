using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayingCardsTable : MonoBehaviour
{
    public static PlayingCardsTable Instance { get; private set; }

    [SerializeField] private RectTransform tableArea;
    [SerializeField] private RectTransform handArea;
    [SerializeField] private GameObject cardPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void StageCard(Card card)
    {
        card.transform.SetParent(tableArea, false);
        card.gameObject.SetActive(false);
    }

    public void ShowOnTable(Card card)
    {
        card.InHand = false;
        card.transform.SetParent(tableArea, false);
        card.gameObject.SetActive(true);
    }

    public void PlaceCardFromHandOnTable(Card card)
    {
        card.InHand = false;
        card.transform.SetParent(tableArea);
    }

    public void ReturnCardToHand(Card card)
    {
        card.ReturnToHand();
        card.transform.SetParent(handArea);
        card.gameObject.SetActive(true);
    }

    public bool IsOverTableArea(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            tableArea,
            screenPosition,
            null);
    }

    public void ClearTable()
    {
        Card[] cardsOnTable = tableArea.GetComponentsInChildren<Card>();
        foreach (Card card in cardsOnTable) 
        {
            Destroy(card.gameObject);
        }
    }
    public void ClearHand()
    {
        Card[] cardsInHand = handArea.GetComponentsInChildren<Card>();
        foreach (var card in cardsInHand)
        {
            Destroy(card.gameObject);
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