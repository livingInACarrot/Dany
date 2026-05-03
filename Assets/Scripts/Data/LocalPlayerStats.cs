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

    private void OnApplicationQuit()
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
        PlayerPrefs.Save();
    }

    private void LoadStats()
    {
        DanyPlays = PlayerPrefs.GetInt("DanyPlays", 0);
        DanyWins = PlayerPrefs.GetInt("DanyWins", 0);
        PersonalityPlays = PlayerPrefs.GetInt("PersonalityPlays", 0);
        PersonalityWins = PlayerPrefs.GetInt("PersonalityWins", 0);
    }
}