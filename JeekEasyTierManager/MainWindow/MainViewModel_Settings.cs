using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
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

        _autoUpdateTimer = new DispatcherTimer();
        _autoUpdateTimer.Tick += OnAutoUpdateMeTimerElapsed;

        AutoUpdateMe = Settings.AutoUpdateMe;
        AutoUpdateEasyTier = Settings.AutoUpdateEasyTier;

        // Apply the periodic update-check interval and start the timer accordingly.
        UpdateInterval = Settings.UpdateCheckInterval;
        ApplyUpdateCheckInterval();
        RefreshUpdateIntervalSelectionProperties();

        SyncPassword = Settings.SyncPassword;

        AutoRefreshInfo = Settings.AutoRefreshInfo;

        RefreshStorageSelectionProperties();

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
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Settings_SetStartOnBootFailed"), ex.Message));
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
    }

    [ObservableProperty]
    public partial bool AutoUpdateEasyTier { get; set; }

    partial void OnAutoUpdateEasyTierChanged(bool value)
    {
        Settings.AutoUpdateEasyTier = value;
        _ = AppSettings.Save(); // Save settings asynchronously
    }

    private DispatcherTimer _autoUpdateTimer = null!;

    [ObservableProperty]
    public partial UpdateCheckInterval UpdateInterval { get; set; }

    partial void OnUpdateIntervalChanged(UpdateCheckInterval value)
    {
        Settings.UpdateCheckInterval = value;
        _ = AppSettings.Save();

        ApplyUpdateCheckInterval();
        RefreshUpdateIntervalSelectionProperties();
    }

    public bool IsUpdateSixHours => UpdateInterval == UpdateCheckInterval.EverySixHours;
    public bool IsUpdateDaily => UpdateInterval == UpdateCheckInterval.Daily;
    public bool IsUpdateWeekly => UpdateInterval == UpdateCheckInterval.Weekly;
    public bool IsUpdateNever => UpdateInterval == UpdateCheckInterval.Never;

    [RelayCommand]
    public void SetUpdateInterval(string interval)
    {
        if (Enum.TryParse<UpdateCheckInterval>(interval, out var parsed))
            UpdateInterval = parsed;
    }

    private void RefreshUpdateIntervalSelectionProperties()
    {
        OnPropertyChanged(nameof(IsUpdateSixHours));
        OnPropertyChanged(nameof(IsUpdateDaily));
        OnPropertyChanged(nameof(IsUpdateWeekly));
        OnPropertyChanged(nameof(IsUpdateNever));
    }

    private void ApplyUpdateCheckInterval()
    {
        var span = UpdateInterval switch
        {
            UpdateCheckInterval.EverySixHours => TimeSpan.FromHours(6),
            UpdateCheckInterval.Daily => TimeSpan.FromDays(1),
            UpdateCheckInterval.Weekly => TimeSpan.FromDays(7),
            _ => TimeSpan.Zero,
        };

        if (span == TimeSpan.Zero)
        {
            _autoUpdateTimer.IsEnabled = false;
        }
        else
        {
            _autoUpdateTimer.Interval = span;
            _autoUpdateTimer.IsEnabled = true;
        }
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
            ClearMessages();
            AddMessage($"Auto update error: {ex.Message}");
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
