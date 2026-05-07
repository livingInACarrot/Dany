using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RulesContainerUI : MonoBehaviour//, IEndDragHandler
{
    [SerializeField] private GameObject rulesPanel;

    private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;

    private RectTransform _firstImage;
    private RectTransform _lastImage;

    private void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
        viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)transform;

        var images = GetComponentsInChildren<RectTransform>();
        _firstImage = images[0];
        _lastImage = images[^1];

        //LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    /*
    private void OnEnable()
    {
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        ClampScrollPosition();
    }

    */
    private void Update()
    {
        if (rulesPanel.activeSelf)
            CheckBoundaries();
            //ClampScrollPosition();
    }

    private void CheckBoundaries()
    {
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        // Дописать
    }




    private void ClampScrollPosition()
    {
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        if (contentHeight < 1f) return;

        if (contentHeight <= viewportHeight)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
            return;
        }

        float maxY = 0f;
        float minY = -(contentHeight - viewportHeight);

        if (content.anchoredPosition.y >= maxY && scrollRect.velocity.y > 0)
            scrollRect.velocity = Vector2.zero;
        else if (content.anchoredPosition.y <= minY && scrollRect.velocity.y < 0)
            scrollRect.velocity = Vector2.zero;

        Vector2 pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        if (pos != content.anchoredPosition)
        {
            content.anchoredPosition = pos;
            scrollRect.velocity = Vector2.zero;
        }
    }
}
