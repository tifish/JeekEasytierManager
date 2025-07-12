global using static JeekEasyTierManager.SettingsContainer;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using JeekTools;

namespace JeekEasyTierManager;

public class AppSettings
{
    public static readonly string AppDirectory = AppContext.BaseDirectory;
    public static readonly string ExePath = Path.Join(AppDirectory, "JeekEasyTierManager.exe");
    public static readonly string EasyTierDirectory = Path.Join(AppDirectory, "EasyTier");
    public static readonly string ConfigDirectory = Path.Join(AppDirectory, "Config");

    public static readonly string EasyTierCorePath = Path.Join(EasyTierDirectory, "easytier-core.exe");
    public static readonly string EasyTierCliPath = Path.Join(EasyTierDirectory, "easytier-cli.exe");
    public static readonly string NssmPath = Path.Join(AppDirectory, "Nssm", "nssm.exe");

    public static readonly string JeekEasyTierManagerZipUrl = "https://github.com/tifish/JeekEasyTierManager/releases/download/latest_release/JeekEasyTierManager.7z";
    public static readonly string EasyTierLatestReleasePageUrl = "https://github.com/EasyTier/EasyTier/releases/latest";

    public static readonly string SettingsDirectory = Path.Combine(AppDirectory, "Settings");
    public static readonly string SettingsFile = Path.Combine(SettingsDirectory, "Settings.json");

    private static readonly JsonFile<AppSettings> _settingsFile = new(SettingsFile);

    public static async Task Load()
    {
        var settings = await _settingsFile.Load();
        if (settings != null)
            Settings = settings;
    }

    public static async Task Save()
    {
        if (Design.IsDesignMode)
            return;

        if (!Directory.Exists(SettingsDirectory))
            Directory.CreateDirectory(SettingsDirectory);

        await _settingsFile.Save(Settings);
    }

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
                _ => ThemeVariant.Default
            };
        }
    }

    public bool DisableMirrorDownload { get; set; } = false;

    public bool AutoUpdateMe { get; set; } = true;
    public bool AutoUpdateEasyTier { get; set; } = false;

    public string SyncPassword { get; set; } = "";

    public bool AutoRefreshInfo { get; set; } = true;
}

public static class SettingsContainer
{
    public static AppSettings Settings = new();
}
