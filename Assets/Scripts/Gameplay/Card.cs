using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Card : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler, IPointerClickHandler
{
    [SerializeField] private Sprite sprite;

    public bool InHand = false;

    public RectTransform rectTransform;
    private Image image;
    private NetworkCard networkCard;
    private Sprite faceSprite;
    private bool isDragging = false;
    public bool isFlipped = false;
    private Vector2 offset;

    public void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        networkCard = GetComponent<NetworkCard>();
        image.sprite = sprite;
        image.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && !InHand)
        {
            if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            {
                StartCoroutine(PlayFlipAnimation());
            }
            else
            {
                if (isFlipped) StartCoroutine(PlayFlipAnimation());
                PlayingCardsTable.Instance.ReturnCardToHand(this);
                networkCard.CmdReturnFromTable();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = true;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPointerPosition))
            {
                offset = rectTransform.anchoredPosition - localPointerPosition;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = false;
            if (InHand)
            {
                if (PlayingCardsTable.Instance.IsOverTableArea(eventData.position))
                {
                    PlayingCardsTable.Instance.PlaceCardFromHandOnTable(this, networkCard);
                }
                else
                {
                    PlayingCardsTable.Instance.ReturnCardToHand(this);
                }
            }
            else
            {
                SendNetworkUpdate();
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && eventData.button == PointerEventData.InputButton.Left)
        {
            FollowPointer(eventData);
            SendNetworkUpdate();
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (InHand) return;

        float scrollDelta = eventData.scrollDelta.y;

        if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            ScaleCard(scrollDelta);
        else
            RotateCard(scrollDelta);

        SendNetworkUpdate();
    }

    public void ChangePosition(Vector2 newPos, Quaternion newRot, Vector3 newScale)
    {
        rectTransform.anchoredPosition = newPos;
        rectTransform.rotation = newRot;
        rectTransform.localScale = newScale;
    }

    public void ReturnToHand()
    {
        rectTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
        rectTransform.localScale = Vector3.one;
        InHand = true;
    }

    public void SetSprite(Sprite newSprite)
    {
        faceSprite = newSprite;
        sprite = newSprite;
        if (!isFlipped)
            image.sprite = newSprite;
    }

    public void FlipCard(bool newIsFlipped)
    {
        isFlipped = newIsFlipped;
        image.sprite = isFlipped ? CardsStorage.PictureCardBackSprite : (faceSprite ?? sprite);
    }

    private void FlipCard()
    {
        isFlipped = !isFlipped;
        image.sprite = isFlipped ? CardsStorage.PictureCardBackSprite : (faceSprite ?? sprite);
    }

    private void SendNetworkUpdate()
    {
        if (!networkCard.isOwned) return;
        networkCard.CmdUpdateCard(
            rectTransform.anchoredPosition,
            transform.eulerAngles.z,
            transform.localScale,
            isFlipped);
    }

    private void RotateCard(float delta)
    {
        float rotationSpeed = 0.5f;
        float currentRotation = transform.rotation.eulerAngles.z;
        if (currentRotation > 180f) currentRotation -= 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, currentRotation + delta * rotationSpeed);
    }

    private void ScaleCard(float delta)
    {
        float minScale = 0.5f;
        float maxScale = 3f;
        float scaleSpeed = 0.01f;

        Vector3 newScale = transform.localScale + new Vector3(delta * scaleSpeed, delta * scaleSpeed, delta * scaleSpeed);
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
        transform.localScale = newScale;
    }

    private void FollowPointer(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition))
        {
            Vector2 newPosition = localPointerPosition + offset;
            rectTransform.anchoredPosition = newPosition;
        }
    }

    private IEnumerator PlayFlipAnimation()
    {
        float flipSpeed = 1000f;
        float targetWidth = rectTransform.rect.width;
        float currentWidth = targetWidth;

        while (currentWidth > 0)
        {
            currentWidth -= flipSpeed * Time.deltaTime;
            currentWidth = Mathf.Max(0, currentWidth);
            rectTransform.sizeDelta = new Vector2(currentWidth, rectTransform.sizeDelta.y);
            yield return null;
        }
        FlipCard();
        while (currentWidth < targetWidth)
        {
            currentWidth += flipSpeed * Time.deltaTime;
            currentWidth = Mathf.Min(targetWidth, currentWidth);
            rectTransform.sizeDelta = new Vector2(currentWidth, rectTransform.sizeDelta.y);
            yield return null;
        }
        if (!InHand) SendNetworkUpdate();
    }
}
