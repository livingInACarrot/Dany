using UnityEngine;

public class PlayingCardsTable : MonoBehaviour
{
    public static PlayingCardsTable Instance { get; private set; }

    [SerializeField] private RectTransform tableArea;
    [SerializeField] private RectTransform handArea;
    [SerializeField] private GameObject cardPrefab;

    private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

    private void Awake()
    {
        Instance = this;
    }

    public void StageCard(Card card)
    {
        card.transform.SetParent(tableArea, false);
        card.rectTransform.anchorMin = CenterAnchor;
        card.rectTransform.anchorMax = CenterAnchor;
        card.gameObject.SetActive(false);
    }

    public void ShowOnTable(Card card)
    {
        card.InHand = false;
        card.transform.SetParent(tableArea, false);
        card.rectTransform.anchorMin = CenterAnchor;
        card.rectTransform.anchorMax = CenterAnchor;
        card.gameObject.SetActive(true);
    }

    public void PlaceCardFromHandOnTable(Card card, NetworkCard networkCard)
    {
        card.InHand = false;
        Vector3 worldPos = card.rectTransform.position;
        card.transform.SetParent(tableArea, false);
        card.rectTransform.anchorMin = CenterAnchor;
        card.rectTransform.anchorMax = CenterAnchor;
        card.rectTransform.position = worldPos;
        if (networkCard != null && networkCard.isOwned)
        {
            networkCard.CmdPlaceOnTable(
                card.rectTransform.anchoredPosition,
                card.transform.eulerAngles.z,
                card.transform.localScale,
                card.isFlipped);
        }
    }

    public void ReturnCardToHand(Card card)
    {
        card.ReturnToHand();
        Vector3 worldPos = card.rectTransform.position;
        card.transform.SetParent(handArea, false);
        card.rectTransform.anchorMin = CenterAnchor;
        card.rectTransform.anchorMax = CenterAnchor;
        card.rectTransform.position = worldPos;
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
            Destroy(card.gameObject);
    }

    public void ClearHand()
    {
        Card[] cardsInHand = handArea.GetComponentsInChildren<Card>();
        foreach (var card in cardsInHand)
            Destroy(card.gameObject);
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
