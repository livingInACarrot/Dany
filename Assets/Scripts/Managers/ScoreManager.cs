using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int PersonalitiesScore { get; set; }
    public int DannyScore { get; set; }

    public event System.Action<int, int> OnScoreChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddPointToPersonalities()
    {
        PersonalitiesScore++;
        OnScoreChanged?.Invoke(PersonalitiesScore, DannyScore);

        ScoreUI.Instance.UpdateScore(PersonalitiesScore, DannyScore);
    }

    public void AddPointToDanny()
    {
        DannyScore++;
        OnScoreChanged?.Invoke(PersonalitiesScore, DannyScore);

        ScoreUI.Instance.UpdateScore(PersonalitiesScore, DannyScore);
    }

    public void ResetScores()
    {
        PersonalitiesScore = 0;
        DannyScore = 0;
        OnScoreChanged?.Invoke(PersonalitiesScore, DannyScore);

        ScoreUI.Instance.ResetScore();
    }
}
