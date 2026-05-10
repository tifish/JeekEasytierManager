using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;
using RegistryHelper = DotNetRun.Reg;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    public partial bool SettingsDialogIsOpen { get; set; }

    [RelayCommand]
    public void OpenSettings()
    {
        SettingsDialogIsOpen = true;
    }

    [RelayCommand]
    public void CloseSettings()
    {
        SettingsDialogIsOpen = false;
    }

    private async Task ApplySettings()
    {
        await ApplyTheme(Settings.Theme, false);

        StartOnBoot = RegistryHelper.GetValue(RunKeyPath, RunValueName, "") == RunValue;

        DisableMirrorDownload = Settings.DisableMirrorDownload;

        _autoUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _autoUpdateTimer.Tick += OnAutoUpdateMeTimerElapsed;

        // The timer will be updated
        AutoUpdateMe = Settings.AutoUpdateMe;
        AutoUpdateEasyTier = Settings.AutoUpdateEasyTier;

        SyncPassword = Settings.SyncPassword;

        AutoRefreshInfo = Settings.AutoRefreshInfo;

        // Check for updates when start
        await CheckForUpdates();
    }

    [RelayCommand]
    public async Task SetTheme(string theme)
    {
        await ApplyTheme(theme, true);
    }

    private string _selectedTheme = "Default";

    public string SelectedTheme
    {
        get => _selectedTheme;
        private set
        {
            if (SetProperty(ref _selectedTheme, value))
                RefreshThemeSelectionProperties();
        }
    }

    public bool IsLightTheme => SelectedTheme == "Light";
    public bool IsDarkTheme => SelectedTheme == "Dark";
    public bool IsSystemTheme => SelectedTheme == "Default";

    private async Task ApplyTheme(string theme, bool save)
    {
        theme = NormalizeTheme(theme);

        Settings.Theme = theme;
        SelectedTheme = theme;

        Application.Current!.RequestedThemeVariant = Settings.ThemeVariant;
        RefreshThemeSelectionProperties();

        if (save)
            await AppSettings.Save();
    }

    private static string NormalizeTheme(string theme)
    {
        return theme switch
        {
            "Light" => "Light",
            "Dark" => "Dark",
            "Default" => "Default",
            _ => "Default",
        };
    }

    private void RefreshThemeSelectionProperties()
    {
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsSystemTheme));
    }

    private const string RunKeyPath =
        @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "JeekEasyTierManager";
    private static readonly string RunValue = $"\"{AppSettings.ExePath}\" /hide";

    [ObservableProperty]
    public partial bool StartOnBoot { get; set; }

    partial void OnStartOnBootChanged(bool value)
    {
        try
        {
            if (value)
            {
                // Add to registry startup
                RegistryHelper.SetValue(RunKeyPath, RunValueName, RunValue);
            }
            else
            {
                // Remove from registry startup
                RegistryHelper.DeleteValue(RunKeyPath, RunValueName);
            }
        }
        catch (Exception ex)
        {
            Messages = $"Failed to set start on boot: {ex.Message}";
        }
    }

    [ObservableProperty]
    public partial bool DisableMirrorDownload { get; set; }

    partial void OnDisableMirrorDownloadChanged(bool value)
    {
        Settings.DisableMirrorDownload = value;
        _ = AppSettings.Save();
    }

    [ObservableProperty]
    public partial bool AutoUpdateMe { get; set; }

    partial void OnAutoUpdateMeChanged(bool value)
    {
        Settings.AutoUpdateMe = value;
        _ = AppSettings.Save(); // Save settings asynchronously

        RefreshAutoUpdateTimer();
    }

    [ObservableProperty]
    public partial bool AutoUpdateEasyTier { get; set; }

    partial void OnAutoUpdateEasyTierChanged(bool value)
    {
        Settings.AutoUpdateEasyTier = value;
        _ = AppSettings.Save(); // Save settings asynchronously

        RefreshAutoUpdateTimer();
    }

    private DispatcherTimer _autoUpdateTimer = null!;

    private void RefreshAutoUpdateTimer()
    {
        _autoUpdateTimer.IsEnabled = AutoUpdateMe || AutoUpdateEasyTier;
    }

    private async void OnAutoUpdateMeTimerElapsed(object? sender, EventArgs e)
    {
        try
        {
            // Only check for update when user do not open the main window
            if (_mainWindow!.IsVisible)
                return;

            await CheckForUpdates();
        }
        catch (Exception ex)
        {
            Messages = $"Auto update error: {ex.Message}";
        }
    }

    private async Task CheckForUpdates()
    {
        if (AutoUpdateEasyTier)
        {
            await UpdateEasyTier(false);
        }

        if (AutoUpdateMe)
        {
            await UpdateMe(false);
        }
    }

    [ObservableProperty]
    public partial string SyncPassword { get; set; }

    partial void OnSyncPasswordChanged(string value)
    {
        Settings.SyncPassword = value;
        _ = AppSettings.Save();
    }

    [ObservableProperty]
    public partial bool ShowSyncPassword { get; set; }

    [ObservableProperty]
    public partial bool DeleteExtraConfigsOnOtherNodesWhenNextSync { get; set; }

    [ObservableProperty]
    public partial bool AutoRefreshInfo { get; set; }

    partial void OnAutoRefreshInfoChanged(bool value)
    {
        Settings.AutoRefreshInfo = value;
        _ = AppSettings.Save();
    }
}
