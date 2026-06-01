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
    private float _naturalWidth;
    private Coroutine _flipCoroutine;

    public void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        networkCard = GetComponent<NetworkCard>();
        image.sprite = sprite;
        image.raycastTarget = true;
        _naturalWidth = rectTransform.sizeDelta.x;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && !InHand)
        {
            if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            {
                StartLocalFlip();
            }
            else
            {
                if (isFlipped)
                {
                    StopFlipAnimation();
                    FlipCard(false);
                }
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
                if (PlayingCardsTable.Instance.IsOverTableArea(eventData.position, eventData.pressEventCamera))
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

    private void StartLocalFlip()
    {
        bool target = !isFlipped;
        StopFlipAnimation();
        _flipCoroutine = StartCoroutine(LocalFlipCoroutine(target));
    }

    private IEnumerator LocalFlipCoroutine(bool targetFlipped)
    {
        yield return PlayFlipAnimationTo(targetFlipped);
        _flipCoroutine = null;
        if (!InHand) SendNetworkUpdate();
    }

    public void TriggerFlipAnimation(bool targetFlipped)
    {
        StopFlipAnimation();
        _flipCoroutine = StartCoroutine(NetworkFlipCoroutine(targetFlipped));
    }

    private IEnumerator NetworkFlipCoroutine(bool targetFlipped)
    {
        yield return PlayFlipAnimationTo(targetFlipped);
        _flipCoroutine = null;
    }

    public void StopFlipAnimation()
    {
        if (_flipCoroutine != null)
        {
            StopCoroutine(_flipCoroutine);
            _flipCoroutine = null;
            rectTransform.sizeDelta = new Vector2(_naturalWidth, rectTransform.sizeDelta.y);
        }
    }

    private IEnumerator PlayFlipAnimationTo(bool targetFlipped)
    {
        float flipSpeed = 1000f;
        float targetWidth = _naturalWidth;
        float currentWidth = targetWidth;

        while (currentWidth > 0)
        {
            currentWidth -= flipSpeed * Time.deltaTime;
            currentWidth = Mathf.Max(0, currentWidth);
            rectTransform.sizeDelta = new Vector2(currentWidth, rectTransform.sizeDelta.y);
            yield return null;
        }
        FlipCard(targetFlipped);
        while (currentWidth < targetWidth)
        {
            currentWidth += flipSpeed * Time.deltaTime;
            currentWidth = Mathf.Min(targetWidth, currentWidth);
            rectTransform.sizeDelta = new Vector2(currentWidth, rectTransform.sizeDelta.y);
            yield return null;
        }
    }

    public const float SweepDuration = 1.5f;

    public void SweepCard()
    {
        StartCoroutine(PlaySweepAnimation());
    }

    private IEnumerator PlaySweepAnimation()
    {
        float elapsed = 0f;
        float shift = Random.Range(-50f, 50f);
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 target = new(startPos.x + shift, startPos.y + Screen.height * 2);

        while (elapsed < SweepDuration)
        {
            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, target, Mathf.Clamp01(elapsed / SweepDuration));
            yield return null;
        }

        rectTransform.anchoredPosition = target;
    }

}
