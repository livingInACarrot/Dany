using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RulesContainerUI : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private GameObject rulesPanel;
    private ScrollRect scrollRect;
    private RectTransform content;

    private void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
    }

    private void Update()
    {
        if (rulesPanel.activeSelf)
            ClampScrollPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ClampScrollPosition();
    }


    private void ClampScrollPosition()
    {
        float contentHeight = content.rect.height;
        float viewportHeight = (transform as RectTransform).rect.height;

        if (contentHeight <= viewportHeight)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
            return;
        }

        float maxY = 0;
        float minY = -(contentHeight - viewportHeight);

        Vector2 pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        if (pos.y != content.anchoredPosition.y)
        {
            content.anchoredPosition = pos;
            scrollRect.velocity = Vector2.zero;
        }
    }
}