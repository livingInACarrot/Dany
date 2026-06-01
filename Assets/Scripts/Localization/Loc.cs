// Обёртка LocalizationManager для удобного использования
public static class Loc
{
    public static string Text(string key)
        => LocalizationManager.Instance.GetText(key);

    public static string Text(string key, string table)
        => LocalizationManager.Instance.GetText(key, table);

    public static string TextFor(NetworkPlayer player, string key)
        => LocalizationManager.Instance.GetTextForLocale(player.LocaleCode, key);

    public static string Nick(int num)
        => $"{Text("voice")} {num}";

    public static string NickFor(NetworkPlayer player, int num)
        => $"{TextFor(player, "voice")} {num}";
}
