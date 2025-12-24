using NUnit.Framework;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler, IPointerClickHandler
{
    [SerializeField] private Sprite sprite;

    public bool InHand = false;
    public float Width => GetComponent<RectTransform>().rect.width;
    public float Height => GetComponent<RectTransform>().rect.height;

    private Canvas canvas;
    private bool isDragging = false;
    private bool isFlipped = false;
    private Vector2 offset;

    public void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        GetComponent<Image>().sprite = sprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer Enter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Pointer Exit");
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
                if (isFlipped)
                    StartCoroutine(PlayFlipAnimation());
                PlayingCardsTable.ReturnCardToHand(this);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = true;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPointerPosition))
            {
                offset = GetComponent<RectTransform>().anchoredPosition - localPointerPosition;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = false;
            if (InHand) PlayingCardsTable.PlaceCardFromHandOnTable(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && eventData.button == PointerEventData.InputButton.Left)
        {
            FollowPointer(eventData);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y;

        if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
        {
            ScaleCard(scrollDelta);
        }
        else if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
        {
            PlayingCardsTable.ChangeCardInOrder(this, -(int)Mathf.Sign(scrollDelta));
        }
        else
        {
            RotateCard(scrollDelta);
        }
    }

    public void ChangeLayer(int newLayer)
    {
        canvas.sortingOrder = newLayer;
    }

    public void ReturnToHand(Vector2 newPos)
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchoredPosition = newPos;
        rect.rotation = Quaternion.Euler(0f, 0f, 0f);
        rect.localScale = Vector3.one;
    }

    // Private methods-helpers
    private void RotateCard(float delta)
    {
        float currentRotation = transform.rotation.eulerAngles.z;
        if (currentRotation > 180f) currentRotation -= 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, currentRotation + delta * PlayingCardsTable.CardsRotationSpeed);
    }

    private void ScaleCard(float delta)
    {
        float scale = delta * PlayingCardsTable.ScaleDuration;
        Vector3 newScale = transform.localScale + new Vector3(scale, scale, scale);
        if (newScale.x >= PlayingCardsTable.MinScale && newScale.y >= PlayingCardsTable.MinScale && newScale.z >= PlayingCardsTable.MinScale &&
            newScale.x <= PlayingCardsTable.MaxScale && newScale.y <= PlayingCardsTable.MaxScale && newScale.z <= PlayingCardsTable.MaxScale)
            transform.localScale = newScale;
    }

    private void FollowPointer(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPointerPosition))
        {
            Vector2 newPosition = localPointerPosition + offset;
            GetComponent<RectTransform>().anchoredPosition = newPosition;
        }
    }

    private void FlipCard()
    {
        isFlipped = !isFlipped;
        Image img = GetComponent<Image>();
        if (isFlipped)
            img.sprite = CardsStorage.PictureCardBackSprite;
        else
            img.sprite = sprite;
    }

    private IEnumerator PlayFlipAnimation()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        float targetWidth = rectTransform.rect.width;
        float currentWidth = targetWidth;

        while (currentWidth > 0)
        {
            currentWidth -= PlayingCardsTable.CardsFlipSpeed * Time.deltaTime;
            currentWidth = Mathf.Max(0, currentWidth);
            rectTransform.sizeDelta = new Vector2(currentWidth, rectTransform.sizeDelta.y);
            yield return null;
        }
        FlipCard();
        while (currentWidth < targetWidth)
        {
            currentWidth += PlayingCardsTable.CardsFlipSpeed * Time.deltaTime;
            currentWidth = Mathf.Min(targetWidth, currentWidth);
            rectTransform.sizeDelta = new Vector2(currentWidth, rectTransform.sizeDelta.y);
            yield return null;
        }
    }
}