using System;
using System.Collections.Generic;
using System.Linq;
using Nett;

namespace JeekEasytierManager;

public static class TomlTableExt
{
    public static T Get<T>(this TomlTable? table, string key, T defaultValue)
    {
        if (table == null)
            return defaultValue;

        if (table.TryGetValue(key, out var value))
        {
            try
            {
                return value.Get<T>();
            }
            catch
            {
                // ignore
            }
        }

        return defaultValue;
    }

    // Values
    public static void Set(this TomlTable table, string key, bool value)
    {
        var tomlValue = table.CreateAttached(value);
        table[key] = tomlValue;
    }

    public static void Set(this TomlTable table, string key, string value)
    {
        var tomlValue = table.CreateAttached(value);
        table[key] = tomlValue;
    }

    public static void Set(this TomlTable table, string key, long value)
    {
        var tomlValue = table.CreateAttached(value);
        table[key] = tomlValue;
    }

    public static void Set(this TomlTable table, string key, double value)
    {
        var tomlValue = table.CreateAttached(value);
        table[key] = tomlValue;
    }

    public static void Set(this TomlTable table, string key, DateTimeOffset value)
    {
        var tomlValue = table.CreateAttached(value);
        table[key] = tomlValue;
    }

    public static void Set(this TomlTable table, string key, TimeSpan value)
    {
        var tomlValue = table.CreateAttached(value);
        table[key] = tomlValue;
    }

    // Table
    public static void Set<T>(this TomlTable table, string key, IDictionary<string, T> tableData, TomlTable.TableTypes type = TomlTable.TableTypes.Default)
    {
        var newTable = table.CreateAttached(tableData, type);
        table[key] = newTable;
    }

    public static void Set(this TomlTable table, string key, object obj, TomlTable.TableTypes type = TomlTable.TableTypes.Default)
    {
        var newTable = table.CreateAttached(obj, type);
        table[key] = newTable;
    }

    // Table Array
    public static void Set<T>(this TomlTable table, string key, IEnumerable<T> items, TomlTable.TableTypes type = TomlTable.TableTypes.Default)
    {
        var tableArray = table.CreateAttached(items, type);
        table[key] = tableArray;
    }

    // Arrays
    public static void Set(this TomlTable table, string key, IEnumerable<bool> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<string> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<long> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<int> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<double> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<float> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<DateTimeOffset> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<DateTime> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    public static void Set(this TomlTable table, string key, IEnumerable<TimeSpan> array)
    {
        var tomlArray = table.CreateAttached(array);
        table[key] = tomlArray;
    }

    // Values with default value check
    public static void Set(this TomlTable table, string key, bool value, bool defaultValue)
    {
        if (value.Equals(defaultValue))
            table.Remove(key);
        else
            table.Set(key, value);
    }

    public static void Set(this TomlTable table, string key, string value, string defaultValue)
    {
        if (value.Equals(defaultValue))
            table.Remove(key);
        else
            table.Set(key, value);
    }

    public static void Set(this TomlTable table, string key, long value, long defaultValue)
    {
        if (value.Equals(defaultValue))
            table.Remove(key);
        else
            table.Set(key, value);
    }

    public static void Set(this TomlTable table, string key, double value, double defaultValue)
    {
        if (value.Equals(defaultValue))
            table.Remove(key);
        else
            table.Set(key, value);
    }

    public static void Set(this TomlTable table, string key, DateTimeOffset value, DateTimeOffset defaultValue)
    {
        if (value.Equals(defaultValue))
            table.Remove(key);
        else
            table.Set(key, value);
    }

    public static void Set(this TomlTable table, string key, TimeSpan value, TimeSpan defaultValue)
    {
        if (value.Equals(defaultValue))
            table.Remove(key);
        else
            table.Set(key, value);
    }

    // Table with default value check
    public static void Set<T>(this TomlTable table, string key, IDictionary<string, T> tableData, IDictionary<string, T> defaultValue, TomlTable.TableTypes type = TomlTable.TableTypes.Default)
    {
        if (tableData.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, tableData, type);
    }

    public static void Set(this TomlTable table, string key, object obj, object defaultValue, TomlTable.TableTypes type = TomlTable.TableTypes.Default)
    {
        if (obj.Equals(defaultValue))
            table.Remove(key);
        else
            table.Set(key, obj, type);
    }

    // Table Array with default value check
    public static void Set<T>(this TomlTable table, string key, IEnumerable<T> items, IEnumerable<T> defaultValue, TomlTable.TableTypes type = TomlTable.TableTypes.Default)
    {
        if (items.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, items, type);
    }

    // Arrays with default value check
    public static void Set(this TomlTable table, string key, IEnumerable<bool> array, IEnumerable<bool> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<string> array, IEnumerable<string> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<long> array, IEnumerable<long> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<int> array, IEnumerable<int> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<double> array, IEnumerable<double> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<float> array, IEnumerable<float> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<DateTimeOffset> array, IEnumerable<DateTimeOffset> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<DateTime> array, IEnumerable<DateTime> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static void Set(this TomlTable table, string key, IEnumerable<TimeSpan> array, IEnumerable<TimeSpan> defaultValue)
    {
        if (array.SequenceEqual(defaultValue))
            table.Remove(key);
        else
            table.Set(key, array);
    }

    public static TomlTable? GetTable(this TomlTable table, string key)
    {
        if (table == null)
            return null;

        if (table.TryGetValue(key, out var value))
        {
            if (value is TomlTable tableValue)
                return tableValue;
        }

        return null;
    }

    public static string GetMultiLinesTextFromArray(this TomlTable table, string key)
    {
        if (table == null)
            return "";

        if (table.TryGetValue(key, out var value))
        {
            var array = (TomlArray)value;
            return string.Join("\n", array.Items.Select(item => item.Get<string>()));
        }
        else
        {
            return "";
        }
    }

    public static void SetMultiLinesTextToArray(this TomlTable table, string tableArrayKey, string value)
    {
        List<string> array = [.. value
            .Split('\n')
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrEmpty(item))
        ];

        table.Set(tableArrayKey, array);
    }

    public static string GetMultiLinesTextFromTableArray(this TomlTable table, string tableArrayKey, string propertyKey)
    {
        if (table == null)
            return "";

        if (table.TryGetValue(tableArrayKey, out var value))
        {
            var tableArray = (TomlTableArray)value;
            return string.Join("\n", tableArray.Items.Select(item => item.Get(propertyKey, "")));
        }
        else
        {
            return "";
        }
    }

    public static void SetMultiLinesTextToTableArray(this TomlTable table, string tableArrayKey, string propertyKey, string value)
    {
        var array = value
            .Split('\n')
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrEmpty(item))
            .ToList();


        var tableArray = table.CreateEmptyAttachedTableArray();

        foreach (var item in array)
        {
            var itemTable = table.CreateEmptyAttachedTable();
            itemTable.Set(propertyKey, item);
            tableArray.Add(itemTable);
        }

        table[tableArrayKey] = tableArray;
    }

}
