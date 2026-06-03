using UnityEngine;
using UnityEngine.UI;

// Letterbox/pillarbox через UI-полосы поверх сцены.
// Камера НЕ трогается — camera.rect всегда (0,0,1,1), что исключает размытость при DPI scaling.
[RequireComponent(typeof(Canvas))]
public class AspectRatioEnforcer : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;

    private Image _barA;
    private Image _barB;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        _barA = CreateBar("BarA");
        _barB = CreateBar("BarB");
    }

    private void Start() => Apply();

    private void Update()
    {
        if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
        Apply();
    }

    private void Apply()
    {
        _lastWidth  = Screen.width;
        _lastHeight = Screen.height;

        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / TargetAspect;

        if (Mathf.Abs(scale - 1f) < 0.001f)
        {
            _barA.gameObject.SetActive(false);
            _barB.gameObject.SetActive(false);
            return;
        }

        _barA.gameObject.SetActive(true);
        _barB.gameObject.SetActive(true);

        if (scale < 1f)
        {
            // letterbox: полосы сверху и снизу
            float barPx = Screen.height * (1f - scale) / 2f;
            PositionBar(_barA, 0f, Screen.height - barPx, Screen.width, barPx); // top
            PositionBar(_barB, 0f, 0f,                    Screen.width, barPx); // bottom
        }
        else
        {
            // pillarbox: полосы слева и справа
            float barPx = Screen.width * (1f - 1f / scale) / 2f;
            PositionBar(_barA, 0f,                   0f, barPx, Screen.height); // left
            PositionBar(_barB, Screen.width - barPx, 0f, barPx, Screen.height); // right
        }
    }

    private static void PositionBar(Image bar, float x, float y, float w, float h)
    {
        RectTransform rt = bar.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot     = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    private Image CreateBar(string barName)
    {
        var go = new GameObject(barName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        var img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return img;
    }
}
