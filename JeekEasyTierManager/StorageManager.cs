using System;
using System.IO;

namespace JeekEasyTierManager;

public enum StorageMode
{
    // Pinned values: persisted to LocalSettings.json, so never reorder.
    Default = 0,
    Portable = 1,
    Custom = 2,
}

/// <summary>
/// Resolves where roaming data (the EasyTier `Config\*.toml` files and the app's `Settings`) lives,
/// based on the active storage mode. The mode itself is machine-specific and stored under
/// %LocalAppData% so it can be read before the roaming location is known.
/// </summary>
public static class StorageManager
{
    public const string AppName = "JeekEasyTierManager";

    // Machine-specific store (never roams). Holds LocalSettings.json (the storage mode choice).
    public static string LocalAppDataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName
        );
    public static string LocalConfigDirectory => Path.Combine(LocalAppDataRoot, "Config");
    public static string LocalSettingsFile => Path.Combine(LocalConfigDirectory, "LocalSettings.json");

    // Roaming root for the AppData (Default) mode.
    public static string AppDataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName
        );

    // Portable probe (next to the exe).
    public static string PortableRoot => AppContext.BaseDirectory.TrimEnd('\\', '/');
    public static string PortableConfigDirectory => Path.Combine(PortableRoot, "Config");
    public static string PortableSettingsDirectory => Path.Combine(PortableRoot, "Settings");

    // Both Config and Settings are portable data for this app, so the presence of either next to
    // the exe marks a portable install.
    public static bool IsPortableDataPresent =>
        Directory.Exists(PortableConfigDirectory) || Directory.Exists(PortableSettingsDirectory);

    // The active roaming location (resolved at startup, updated on a mode switch).
    public static StorageMode ActiveMode { get; private set; } = StorageMode.Default;
    public static string ActiveRoamingRoot { get; private set; } = "";

    public static string ActiveConfigDirectory => Path.Combine(ActiveRoamingRoot, "Config");
    public static string ActiveSettingsDirectory => Path.Combine(ActiveRoamingRoot, "Settings");
    public static string ActiveSettingsFile => Path.Combine(ActiveSettingsDirectory, "Settings.json");

    public static string GetRoamingRoot(StorageMode mode, string? customDirectory)
    {
        return mode switch
        {
            StorageMode.Portable => PortableRoot,
            StorageMode.Custom => (customDirectory ?? "").TrimEnd('\\', '/'),
            _ => AppDataRoot,
        };
    }

    public static void SetActive(StorageMode mode, string roamingRoot)
    {
        ActiveMode = mode;
        ActiveRoamingRoot = roamingRoot;
        Directory.CreateDirectory(ActiveConfigDirectory);
        Directory.CreateDirectory(ActiveSettingsDirectory);
    }

    /// <summary>
    /// Decides the active storage location at startup. Portable data next to the exe always wins
    /// (forced portable). Otherwise honor the saved mode, falling back to Default (AppData).
    /// Requires <see cref="LocalSettings.Current"/> to be loaded first.
    /// </summary>
    public static void Resolve()
    {
        var local = LocalSettings.Current;

        StorageMode mode;
        string root;

        if (IsPortableDataPresent)
        {
            mode = StorageMode.Portable;
            root = PortableRoot;
        }
        else if (
            local.StorageMode == StorageMode.Custom
            && !string.IsNullOrWhiteSpace(local.CustomDirectory)
            && IsUsableDirectory(local.CustomDirectory)
        )
        {
            mode = StorageMode.Custom;
            root = local.CustomDirectory.TrimEnd('\\', '/');
        }
        else if (local.StorageMode == StorageMode.Portable)
        {
            mode = StorageMode.Portable;
            root = PortableRoot;
        }
        else
        {
            mode = StorageMode.Default;
            root = AppDataRoot;
        }

        SetActive(mode, root);
    }

    /// <summary>
    /// True if the directory exists, or can be created and written to.
    /// </summary>
    public static bool IsUsableDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probe = Path.Combine(directory, ".write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
