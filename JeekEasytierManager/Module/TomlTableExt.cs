using Tomlyn.Model;
using System;
using System.Linq;

namespace JeekEasytierManager;

public static class TomlTableExt
{
    public static void Set(this TomlTable table, string key, object value)
    {
        table.Remove(key);
        table.Add(key, value);
    }

    public static void Set<T>(this TomlTable table, string key, T value, T defaultValue) where T : IEquatable<T>
    {
        table.Remove(key);

        if (!value.Equals(defaultValue))
            table.Add(key, value);
    }

    public static T Get<T>(this TomlTable table, string key, T defaultValue)
    {
        if (table.TryGetValue(key, out var value))
            return (T)value;
        else
            return defaultValue;
    }

    public static TomlTable? GetTable(this TomlTable table, string key)
    {
        if (table.TryGetValue(key, out var value))
            return (TomlTable)value;
        else
            return null;
    }

    public static string? GetMultiLinesTextFromArray(this TomlTable table, string key)
    {
        if (table.TryGetValue(key, out var value))
        {
            var array = (TomlArray)value;
            return string.Join("\n", array.Select(item => (string)item!));
        }
        else
        {
            return null;
        }
    }

    public static void SetMultiLinesTextToArray(this TomlTable table, string tableArrayKey, string value)
    {
        var array = value
            .Split('\n')
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrEmpty(item))
            .ToArray();

        var tomlArray = new TomlArray(array.Length);
        foreach (var item in array)
            tomlArray.Add(item);

        table.Set(tableArrayKey, tomlArray);
    }

    public static string? GetMultiLinesTextFromTableArray(this TomlTable table, string tableArrayKey, string propertyKey)
    {
        if (table.TryGetValue(tableArrayKey, out var value))
        {
            var tableArray = (TomlTableArray)value;
            return string.Join("\n", tableArray.Select(item => item.Get(propertyKey, "")));
        }
        else
        {
            return null;
        }
    }

    public static void SetMultiLinesTextToTableArray(this TomlTable table, string tableArrayKey, string propertyKey, string value)
    {
        var array = value
            .Split('\n')
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrEmpty(item))
            .ToArray();

        var tomlArray = new TomlTableArray();
        foreach (var item in array)
        {
            tomlArray.Add(new TomlTable { [propertyKey] = item });
        }

        table.Set(tableArrayKey, tomlArray);
    }
}
