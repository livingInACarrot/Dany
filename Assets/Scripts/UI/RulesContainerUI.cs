using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RulesContainerUI : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform content;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>() ?? GetComponentInParent<ScrollRect>(true);
        if (scrollRect != null)
        {
            content = scrollRect.content;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(InitScroll());
    }

    private IEnumerator InitScroll()
    {
        yield return null;
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }
}
