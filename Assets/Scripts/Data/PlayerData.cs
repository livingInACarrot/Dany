using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    private static int nextId = 1;
    private static readonly object lockObject = new();

    public int Id { get; private set; }
    public string Country { get; set; }
    public int PersonalityWins { get; private set; }
    public int DannyWins { get; private set; }
    public int PersonalityPlays { get; private set; }
    public int DannyPlays { get; private set; }

    public uint NetworkId;

    public PlayerData()
    {
        lock (lockObject)
        {
            Id = nextId++;
        }
        Country = "Россия";
        PersonalityWins = 0;
        DannyWins = 0;
        PersonalityPlays = 0;
        DannyPlays = 0;
    }

    public PlayerData(string country = "Россия")
    {
        lock (lockObject)
        {
            Id = nextId++;
        }
        Country = country;
        PersonalityWins = 0;
        DannyWins = 0;
        PersonalityPlays = 0;
        DannyPlays = 0;
    }
}