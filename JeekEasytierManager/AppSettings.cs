global using static JeekEasytierManager.SettingsContainer;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using JeekTools;

namespace JeekEasytierManager;

public class AppSettings
{
    public static readonly string AppDirectory = AppContext.BaseDirectory;
    public static readonly string ExePath = Path.Join(AppDirectory, "JeekEasytierManager.exe");
    public static readonly string EasytierDirectory = Path.Join(AppDirectory, "Easytier");
    public static readonly string ConfigDirectory = Path.Join(AppDirectory, "Config");

    public static readonly string EasytierCorePath = Path.Join(EasytierDirectory, "easytier-core.exe");
    public static readonly string EasytierCliPath = Path.Join(EasytierDirectory, "easytier-cli.exe");
    public static readonly string NssmPath = Path.Join(AppDirectory, "Nssm", "nssm.exe");

    public static readonly string JeekEasytierManagerZipUrl = "https://github.com/tifish/JeekEasytierManager/releases/download/latest_release/JeekEasytierManager.7z";
    public static readonly string EasytierLatestReleasePageUrl = "https://github.com/EasyTier/EasyTier/releases/latest";

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
    public bool AutoUpdateEasytier { get; set; } = false;

    public string SyncPassword { get; set; } = "";

    public bool AutoRefreshInfo { get; set; } = true;
}

public static class SettingsContainer
{
    public static AppSettings Settings = new();
}
