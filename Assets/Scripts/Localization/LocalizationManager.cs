using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private string mainTableName = "UI Labels";
    [SerializeField] private List<string> tableNames;

    private StringTable mainTable;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        LoadTable(mainTableName);
    }

    private void LoadTable(string tableName)
    {
        var tableOperation = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
        tableOperation.Completed += op =>
        {
            mainTable = op.Result;
            if (mainTable == null)
            {
                Debug.LogError($"LocalizationManager: Table '{tableName}' not found!");
            }
        };
    }

    public string GetText(string key)
    {
        return GetText(key, mainTableName);
    }

    public string GetText(string key, string tableName)
    {
        var table = GetTable(tableName);
        if (table == null) return key;
        var entry = table.GetEntry(key);
        return entry != null ? entry.GetLocalizedString() : key;
    }

    private StringTable GetTable(string tableName)
    {
        if (mainTable != null && tableName == mainTableName)
            return mainTable;
        var operation = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
        if (operation.IsDone)
            return operation.Result;
        return null;
    }

    public void SetLocale(UnityEngine.Localization.Locale locale)
    {
        LocalizationSettings.SelectedLocale = locale;
    }

    public void SetLocale(int localeIndex)
    {
        if (localeIndex >= 0 && localeIndex < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
        }
    }

    public string GetStringLocale()
    {
        return LocalizationSettings.SelectedLocale.name;
    }
}