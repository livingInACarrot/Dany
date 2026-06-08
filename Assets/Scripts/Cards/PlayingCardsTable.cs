using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayingCardsTable : MonoBehaviour
{
    public static PlayingCardsTable Instance { get; private set; }

    [SerializeField] private RectTransform tableArea;
    [SerializeField] private RectTransform handArea;
    [SerializeField] private GameObject cardPrefab;

    private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

    private Vector2 _handRestPosition;

    private void Awake()
    {
        Instance = this;
        _handRestPosition = handArea.anchoredPosition;
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
        card.transform.SetAsLastSibling();
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
        card.rectTransform.pivot = CenterAnchor;
        card.rectTransform.position = worldPos;
        card.transform.SetAsLastSibling();
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

    public bool IsOverTableArea(Vector2 screenPosition, Camera cam = null)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            tableArea,
            screenPosition,
            cam);
    }

    public void ToggleInteractions(bool active)
    {
        NetworkCard[] cardsOnTable = tableArea.GetComponentsInChildren<NetworkCard>(true);
        foreach (var card in cardsOnTable)
        {
            if (card.IsOwnedByLocalPlayer())
                card.GetComponent<UnityEngine.UI.Image>().raycastTarget = active;
        }
    }

    public void SweepAllTableCards()
    {
        Card[] cardsOnTable = tableArea.GetComponentsInChildren<Card>();
        foreach (var card in cardsOnTable)
            card.SweepCard();
    }

    public void ClearTable()
    {
        Card[] cardsOnTable = tableArea.GetComponentsInChildren<Card>();
        foreach (var card in cardsOnTable)
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

    public void PopdownHand()
    {
        handArea.anchoredPosition = new(_handRestPosition.x, _handRestPosition.y - handArea.rect.height);
    }

    public void PopupHand()
    {
        StartCoroutine(PopupHandCoroutine());
    }

    private IEnumerator PopupHandCoroutine()
    {
        float elapsed = 0f;
        float duration = 0.5f;
        Vector2 startPos = handArea.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            handArea.anchoredPosition = Vector2.Lerp(startPos, _handRestPosition, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        handArea.anchoredPosition = _handRestPosition;
    }
}
