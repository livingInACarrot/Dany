using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TimerUI : MonoBehaviour
{
    public static TimerUI Instance { get; private set; }

    [SerializeField] private GameObject timerPanel;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color warningColor = Color.darkRed;
    [SerializeField] private float warningThreshold = 10f;

    private Coroutine timerCoroutine;
    private float currentTime;
    private System.Action onTimerEnd;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        timerPanel.SetActive(false);
    }

    public void StartTimer(float seconds, System.Action onComplete = null)
    {
        StopTimer();

        currentTime = seconds;
        onTimerEnd = onComplete;

        timerPanel.SetActive(true);
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();
            yield return null;
        }

        currentTime = 0;
        UpdateTimerDisplay();

        timerPanel.SetActive(false);
        onTimerEnd?.Invoke();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
        timerText.color = currentTime <= warningThreshold ? warningColor : normalColor;
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        timerPanel.SetActive(false);
    }
}
