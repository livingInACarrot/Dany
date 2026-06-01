using Mirror;

public static class IdeasCardReaderWriter
{
    public static void WriteIdeasCard(this NetworkWriter writer, IdeasCard value)
    {
        writer.WriteString(value?.Key ?? "");
    }

    public static IdeasCard ReadIdeasCard(this NetworkReader reader)
    {
        string key = reader.ReadString();
        return string.IsNullOrEmpty(key) ? null : new IdeasCard(key);
    }
}
