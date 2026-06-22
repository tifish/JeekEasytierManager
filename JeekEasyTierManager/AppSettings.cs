global using static JeekEasyTierManager.SettingsContainer;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using Json.Easy;

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
        "https://github.com/tifish/JeekEasyTierManager/releases/download/latest_release/JeekEasyTierManager.7z";
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

    public static async Task Load()
    {
        var settings = await new JsonFile(SettingsFile).Load<AppSettings>();
        if (settings != null)
            Settings = settings;
    }

    public static async Task Save()
    {
        if (Design.IsDesignMode)
            return;

        Directory.CreateDirectory(SettingsDirectory);

        await new JsonFile(SettingsFile).Save(Settings);
    }

    public string Language { get; set; } = "en";

    public string Theme { get; set; } = "Default";

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
