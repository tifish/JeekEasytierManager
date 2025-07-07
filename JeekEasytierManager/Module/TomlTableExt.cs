using Tomlyn.Model;

namespace JeekEasytierManager;

public static class TomlTableExt
{
    public static void Set(this TomlTable table, string key, object value)
    {
        table.Remove(key);
        table.Add(key, value);
    }

    public static T Get<T>(this TomlTable table, string key, T defaultValue)
    {
        if (table.TryGetValue(key, out var value))
            return (T)value;
        else
            return defaultValue;
    }
}