using UnityEngine;

[System.Serializable]
public class LocalPlayerStats : MonoBehaviour
{
    public static LocalPlayerStats Instance { get; private set; }
    public int PersonalityWins { get; private set; }
    public int DanyWins { get; private set; }
    public int PersonalityPlays { get; private set; }
    public int DanyPlays { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadStats();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SaveStats();
    }

    public void AddGame(bool wasDany, bool won)
    {
        if (wasDany)
        {
            ++DanyPlays;
            if (won)
                ++DanyWins;
        }
        else
        {
            ++PersonalityPlays;
            if (won)
                ++PersonalityWins;
        }
        SaveStats();
    }

    private void SaveStats()
    {
        PlayerPrefs.SetInt("DanyPlays", DanyPlays);
        PlayerPrefs.SetInt("DanyWins", DanyWins);
        PlayerPrefs.SetInt("PersonalityPlays", PersonalityPlays);
        PlayerPrefs.SetInt("PersonalityWins", PersonalityWins);
        PlayerPrefs.SetInt("StatsChecksum", ComputeChecksum(DanyPlays, DanyWins, PersonalityPlays, PersonalityWins));
        PlayerPrefs.Save();
    }

    private void LoadStats()
    {
        int dp = PlayerPrefs.GetInt("DanyPlays", 0);
        int dw = PlayerPrefs.GetInt("DanyWins", 0);
        int pp = PlayerPrefs.GetInt("PersonalityPlays", 0);
        int pw = PlayerPrefs.GetInt("PersonalityWins", 0);
        int savedChecksum = PlayerPrefs.GetInt("StatsChecksum", 0);

        if (savedChecksum != 0 && savedChecksum != ComputeChecksum(dp, dw, pp, pw))
        {
            Debug.LogWarning("[Stats] Integrity check failed — resetting stats.");
            return;
        }

        DanyPlays = dp;
        DanyWins = dw;
        PersonalityPlays = pp;
        PersonalityWins = pw;
    }

    private static int ComputeChecksum(int dp, int dw, int pp, int pw)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + dp;
            h = h * 31 + dw;
            h = h * 31 + pp;
            h = h * 31 + pw;
            h = h * 31 + 0x44414e59; // "DANY"
            return h;
        }
    }
}