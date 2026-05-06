using System.Collections;
using TMPro;
using UnityEngine;

public class PopupUI : MonoBehaviour
{
    public static PopupUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float fadeDuration = 0.4f;

    private Coroutine _current;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
    }

    public void Show(string message, float duration = 4f)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        messageText.text = message;
        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(Mathf.Max(0f, duration - fadeDuration));
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - elapsed / fadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        _current = null;
    }
}
