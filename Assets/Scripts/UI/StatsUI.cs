using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    public static StatsUI Instance { get; private set; }

    [SerializeField] private GameObject danyStats;
    [SerializeField] private GameObject persStats;
    [SerializeField] private GameObject allStats;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateAllStats();
    }

    public void UpdateAllStats()
    {
        if (LocalPlayerStats.Instance == null) return;
        UpdateStats(danyStats, LocalPlayerStats.Instance.DanyWins, LocalPlayerStats.Instance.DanyPlays);
        UpdateStats(persStats, LocalPlayerStats.Instance.PersonalityWins, LocalPlayerStats.Instance.PersonalityPlays);
        UpdateStats(allStats, LocalPlayerStats.Instance.PersonalityWins + LocalPlayerStats.Instance.DanyWins,
            LocalPlayerStats.Instance.PersonalityPlays + LocalPlayerStats.Instance.DanyPlays);
    }

    private void UpdateStats(GameObject stats, int wins, int plays)
    {
        if (plays == 0)
        {
            stats.GetComponentsInChildren<TMP_Text>()[1].text = $"0";
            stats.GetComponentsInChildren<TMP_Text>()[2].text = $"";
            return;
        }
        stats.GetComponentsInChildren<TMP_Text>()[1].text = $"{wins}/{plays}";
        stats.GetComponentsInChildren<TMP_Text>()[2].text = $"({Mathf.FloorToInt((float)wins / plays * 100)}%)";
    }
}
