using System.Collections;
using TMPro;
using UnityEngine;

public class GamePopup : MonoBehaviour
{
    public static GamePopup Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float fadeDuration = 0.2f;

    private Coroutine _current;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show(string message, float duration = 6f)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ShowRoutine(message, duration));
    }

    public void ShowPersistent(string message)
    {
        if (_current != null) StopCoroutine(_current);
        messageText.text = message;
        canvasGroup.alpha = 1f;
        _current = null;
    }

    public void Hide()
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(FadeOutRoutine());
    }

    public void Abort()
    {
        if (_current != null) StopCoroutine(_current);
        _current = null;
        canvasGroup.alpha = 0f;
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        messageText.text = message;
        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(Mathf.Max(0f, duration - fadeDuration));
        _current = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
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
