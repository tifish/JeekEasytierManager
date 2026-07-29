global using static JeekEasyTierManager.SettingsContainer;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using JeekTools;

namespace JeekEasyTierManager;

public enum UpdateCheckInterval
{
    Never,
    EverySixHours,
    Daily,
    Weekly,
}

public class AppSettings
{
    public static readonly string AppDirectory = AppContext.BaseDirectory;
    public static readonly string ExePath = Path.Join(AppDirectory, "JeekEasyTierManager.exe");
    public static readonly string EasyTierDirectory = Path.Join(AppDirectory, "EasyTier");

    public static readonly string EasyTierCorePath = Path.Join(
        EasyTierDirectory,
        "easytier-core.exe"
    );
    public static readonly string EasyTierCliPath = Path.Join(
        EasyTierDirectory,
        "easytier-cli.exe"
    );
    public static readonly string NssmPath = Path.Join(AppDirectory, "Nssm", "nssm.exe");

    public static readonly string JeekEasyTierManagerZipUrl =
        "https://github.com/tifish/JeekEasyTierManager/releases/download/latest_release/JeekEasyTierManager.zip";
    public static readonly string JeekEasyTierManagerVersionTxtUrl =
        "https://github.com/tifish/JeekEasyTierManager/releases/download/latest_release/version.txt";
    public static readonly string EasyTierLatestReleasePageUrl =
        "https://github.com/EasyTier/EasyTier/releases/latest";
    public static readonly string HomePageUrl =
        "https://github.com/tifish/JeekEasyTierManager";

    // The roaming Config/Settings locations are resolved at runtime by StorageManager so they follow
    // the active storage mode (Default / Portable / Custom).
    public static string ConfigDirectory => StorageManager.ActiveConfigDirectory;
    public static string SettingsDirectory => StorageManager.ActiveSettingsDirectory;
    public static string SettingsFile => StorageManager.ActiveSettingsFile;

    private static AppSettings _baseline = new();

    public static async Task Load()
    {
        await Task.Run(() =>
        {
            if (JsonSettingsFile.TryLoad(SettingsFile, out AppSettings settings))
            {
                Settings = settings;
                _baseline = JsonSettingsFile.Clone(settings);
            }
        });
    }

    public static async Task Save()
    {
        if (Design.IsDesignMode)
            return;

        var path = SettingsFile;
        await Task.Run(() =>
        {
            Directory.CreateDirectory(SettingsDirectory);
            StorageManager.TouchSelfWrite();
            if (
                JsonSettingsFile.TryMergeAndWrite(
                    path,
                    _baseline,
                    Settings,
                    static _ => { },
                    forceAllLocal: false,
                    out var merged
                )
            )
            {
                Settings = merged;
                _baseline = JsonSettingsFile.Clone(merged);
                StorageManager.TouchSelfWrite();
            }
        });
    }

    public string Language { get; set; } = "en";

    public string Theme { get; set; } = "Default";

    [JsonIgnore]
    public ThemeVariant ThemeVariant
    {
        get
        {
            return Theme switch
            {
                "Default" => ThemeVariant.Default,
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }

    public bool DisableMirrorDownload { get; set; } = false;

    public bool AutoUpdateMe { get; set; } = true;
    public bool AutoUpdateEasyTier { get; set; } = false;

    public UpdateCheckInterval UpdateCheckInterval { get; set; } = UpdateCheckInterval.Daily;

    public string SyncPassword { get; set; } = "";

    public bool AutoRefreshInfo { get; set; } = true;
}

public static class SettingsContainer
{
    public static AppSettings Settings = new();
}
