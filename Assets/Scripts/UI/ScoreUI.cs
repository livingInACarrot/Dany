using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI personalitiesScoreText;
    [SerializeField] private TextMeshProUGUI dannyScoreText;
    [SerializeField] private int maxPersonalitiesScore = 6;
    [SerializeField] private int maxDannyScore = 3;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateScore(int personalitiesScore, int dannyScore)
    {
        personalitiesScoreText.text = $"{personalitiesScore}/{maxPersonalitiesScore}";
        dannyScoreText.text = $"{dannyScore}/{maxDannyScore}";
    }

    public void ResetScore()
    {
        UpdateScore(0, 0);
    }
}
