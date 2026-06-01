using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;

    private Camera _cam;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Start()
    {
        Apply();
    }

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

        Rect rect;
        if (scale < 1f)
        {
            rect = new Rect(0f, (1f - scale) / 2f, 1f, scale);
        }
        else
        {
            float w = 1f / scale;
            rect = new Rect((1f - w) / 2f, 0f, w, 1f);
        }

        _cam.rect = rect;
    }
}
