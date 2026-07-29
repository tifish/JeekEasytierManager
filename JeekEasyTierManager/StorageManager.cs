using System;
using System.IO;
using JeekTools;

namespace JeekEasyTierManager;

/// <summary>
/// Resolves where roaming data (the EasyTier `Config\*.toml` files and the app's `Settings`) lives,
/// on top of the generic <see cref="SettingsStorage"/> path scheme from JeekTools. The location
/// choice itself is machine-specific and stored under %LocalAppData% so it can be read before the
/// roaming location is known.
/// </summary>
public static class StorageManager
{
    public const string AppName = "JeekEasyTierManager";

    public static readonly SettingsStorage Storage = new(AppName);

    // Machine-specific store (never roams). Holds LocalSettings.json (the storage location choice).
    public static string LocalConfigDirectory => Storage.LocalConfigDir;
    public static string LocalSettingsFile =>
        Path.Combine(LocalConfigDirectory, "LocalSettings.json");

    // Portable probe (next to the exe).
    public static string PortableRoot => Storage.ProgramDir.TrimEnd('\\', '/');
    public static string PortableConfigDirectory => Storage.ProgramConfigDir;
    public static string PortableSettingsDirectory => Path.Combine(PortableRoot, "Settings");

    // Both Config and Settings are portable data for this app, so the presence of either next to
    // the exe marks a portable install.
    public static bool IsPortableDataPresent =>
        Storage.ProgramConfigRootExists() || Directory.Exists(PortableSettingsDirectory);

    // The active roaming location (resolved at startup, updated on a mode switch).
    public static StorageLocation ActiveLocation { get; private set; } =
        StorageLocation.UserDirectory;
    public static string ActiveRoamingRoot { get; private set; } = "";

    public static string ActiveConfigDirectory => Path.Combine(ActiveRoamingRoot, "Config");
    public static string ActiveSettingsDirectory => Path.Combine(ActiveRoamingRoot, "Settings");
    public static string ActiveSettingsFile => Path.Combine(ActiveSettingsDirectory, "Settings.json");

    public static string GetRoamingRoot(StorageLocation location, string? customDirectory)
    {
        return location switch
        {
            StorageLocation.ProgramDirectory => PortableRoot,
            StorageLocation.CustomDirectory => (customDirectory ?? "").TrimEnd('\\', '/'),
            _ => Storage.RoamingDir,
        };
    }

    public static void SetActive(StorageLocation location, string roamingRoot)
    {
        ActiveLocation = location;
        ActiveRoamingRoot = roamingRoot;
        Directory.CreateDirectory(ActiveConfigDirectory);
        Directory.CreateDirectory(ActiveSettingsDirectory);
    }

    /// <summary>
    /// Decides the active storage location at startup. Portable data next to the exe always wins
    /// (forced portable). Otherwise honor the saved location, falling back to the user directory.
    /// Requires <see cref="LocalSettings.Current"/> to be loaded first.
    /// </summary>
    public static void Resolve()
    {
        var local = LocalSettings.Current;

        StorageLocation location;
        string root;

        if (IsPortableDataPresent)
        {
            location = StorageLocation.ProgramDirectory;
            root = PortableRoot;
        }
        else if (
            local.StorageMode == StorageLocation.CustomDirectory
            && !string.IsNullOrWhiteSpace(local.CustomDirectory)
            && IsUsableDirectory(local.CustomDirectory)
        )
        {
            location = StorageLocation.CustomDirectory;
            root = local.CustomDirectory.TrimEnd('\\', '/');
        }
        else if (local.StorageMode == StorageLocation.ProgramDirectory)
        {
            location = StorageLocation.ProgramDirectory;
            root = PortableRoot;
        }
        else
        {
            location = StorageLocation.UserDirectory;
            root = Storage.RoamingDir;
        }

        SetActive(location, root);
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

    // Writes done by this process, so the roaming-data watcher can tell its own saves apart from
    // external edits.
    public static long LastSelfWriteTick { get; private set; }

    public static void TouchSelfWrite() => LastSelfWriteTick = Environment.TickCount64;

    public static bool IsSelfWriteRecent(long withinMs = 1000) =>
        LastSelfWriteTick > 0 && Environment.TickCount64 - LastSelfWriteTick < withinMs;
}
