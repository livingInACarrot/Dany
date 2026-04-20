// Надстройка над LocalizationManager для удобства использования (писать меньше)
public static class Loc
{
    public static string Text(string key)
    {
        return LocalizationManager.Instance.GetText(key);
    }
    public static string Text(string key, string table)
    {
        return LocalizationManager.Instance.GetText(key, table);
    }
    public static string Nick(int num)
    {
        return $"{Text("voice")} {num}";
    }
}
