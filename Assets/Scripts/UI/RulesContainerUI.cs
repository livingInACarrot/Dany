using UnityEngine;
using UnityEngine.UI;

public class RulesContainerUI : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;

    private void Start()
    {
        scrollRect = GetComponent<ScrollRect>() ?? GetComponentInParent<ScrollRect>();
        if (scrollRect == null) return;

        content = scrollRect.content;
        viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void OnEnable()
    {
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void Update()
    {
        if (scrollRect == null) return;
        ClampScrollPosition();
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
