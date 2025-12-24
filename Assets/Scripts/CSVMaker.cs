using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class CSVWriter : MonoBehaviour
{
    public void Start()
    {
        var data = new List<string[]>();
        string[] st = {"key", "eng", "rus", "fr", "cn" };
        data.Add(st);
        WriteDataToCSV("Assets/Resources/localization.csv", data);
    }
    public void WriteDataToCSV(string filePath, List<string[]> data)
    {
        using StreamWriter writer = new StreamWriter(filePath);
        foreach (var row in data)
        {
            writer.WriteLine(string.Join(",", row));
        }
        Debug.Log("Done");
    }
}
