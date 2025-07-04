using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private void LoadSettings()
    {
        Application.Current!.RequestedThemeVariant = Settings.ThemeVariant;

        StartOnBoot = RegistryHelper.GetValue(RunKeyPath, RunValueName, "") == RunValue;

        _autoUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _autoUpdateTimer.Tick += OnAutoUpdateMeTimerElapsed;

        AutoUpdateMe = Settings.AutoUpdateMe;
        AutoUpdateEasytier = Settings.AutoUpdateEasytier;
    }

    [RelayCommand]
    public async Task SwitchTheme()
    {
        if (Application.Current is null)
            return;

        string theme;

        // If the theme is default, switch to the opposite theme
        var requestedTheme = Application.Current.RequestedThemeVariant;
        if (requestedTheme == ThemeVariant.Default)
        {
            var actualTheme = Application.Current.ActualThemeVariant;
            theme = actualTheme == ThemeVariant.Dark ? "Light" : "Dark";
        }
        else // If the theme is not default, switch to default
        {
            theme = "Default";
        }

        Settings.Theme = theme;
        Application.Current.RequestedThemeVariant = Settings.ThemeVariant;

        await AppSettings.Save();
    }

    private const string RunKeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "JeekEasytierManager";
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
    public partial bool AutoUpdateMe { get; set; }

    partial void OnAutoUpdateMeChanged(bool value)
    {
        Settings.AutoUpdateMe = value;
        _ = AppSettings.Save(); // Save settings asynchronously

        RefreshAutoUpdateTimer();
    }

    [ObservableProperty]
    public partial bool AutoUpdateEasytier { get; set; }

    partial void OnAutoUpdateEasytierChanged(bool value)
    {
        Settings.AutoUpdateEasytier = value;
        _ = AppSettings.Save(); // Save settings asynchronously

        RefreshAutoUpdateTimer();
    }

    private DispatcherTimer _autoUpdateTimer = null!;

    private void RefreshAutoUpdateTimer()
    {
        _autoUpdateTimer.IsEnabled = AutoUpdateMe || AutoUpdateEasytier;
    }

    private async void OnAutoUpdateMeTimerElapsed(object? sender, EventArgs e)
    {
        try
        {
            if (AutoUpdateEasytier)
            {
                await UpdateEasytier();
            }

            if (AutoUpdateMe)
            {
                await UpdateMe();
            }
        }
        catch (Exception ex)
        {
            Messages = $"Auto update error: {ex.Message}";
        }
    }

}