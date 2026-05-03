using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI personalitiesScoreText;
    [SerializeField] private TextMeshProUGUI danyScoreText;
    [SerializeField] private int maxPersonalitiesScore = 6;
    [SerializeField] private int maxDanyScore = 3;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateScore(int personalitiesScore, int danyScore)
    {
        personalitiesScoreText.text = $"{personalitiesScore}/{maxPersonalitiesScore}";
        danyScoreText.text = $"{danyScore}/{maxDanyScore}";
    }

    public void ResetScore()
    {
        UpdateScore(0, 0);
    }
}
