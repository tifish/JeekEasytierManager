using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;
using Microsoft.Win32;
using Nett;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public static MainViewModel Instance { get; private set; } = new();

    [ObservableProperty]
    public partial string Title { get; set; } = "Jeek Easytier Manager";

    [ObservableProperty]
    public partial ObservableCollection<ConfigInfo> Configs { get; set; } = [];

    public async Task Init()
    {
        await AppSettings.Load();
        await LoadConfigs();
        await UpdateServiceStatus();
        CheckHasEasytier();
        await ShowPeers();

        _autoUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _autoUpdateTimer.Tick += OnAutoUpdateMeTimerElapsed;

        AutoUpdateMe = Settings.AutoUpdateMe;
        AutoUpdateEasytier = Settings.AutoUpdateEasytier;
    }

    private async Task LoadConfigs()
    {
        if (!Directory.Exists(AppSettings.ConfigDirectory))
            return;

        // 获取配置文件列表
        var configFiles = Directory.GetFiles(AppSettings.ConfigDirectory, "*.toml");
        foreach (var configFile in configFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(configFile);
            Configs.Add(new ConfigInfo { Name = fileName });
        }

        // 获取系统服务列表，找到 ServicePrefix 开头的服务
        var output = await Nssm.RunWithOutput("sc", "query state= all");
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("SERVICE_NAME:"))
            {
                var serviceName = line.Split(':')[1].Trim();
                if (serviceName.StartsWith(ServicePrefix))
                {
                    var configName = serviceName[ServicePrefix.Length..];
                    var config = Configs.FirstOrDefault(c => c.Name == configName);
                    if (config != null)
                        config.Enabled = true;
                }
            }
        }
    }

    private const string ServicePrefix = "Easytier ";

    [RelayCommand]
    public async Task InstallService()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.Enabled)
                continue;

            var configPath = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
            await Nssm.InstallService(ServicePrefix + config.Name, AppSettings.EasytierCorePath, $"-c \"{configPath}\"");
        }

        await RestartService();
        await UpdateServiceStatus();
    }

    [RelayCommand]
    public async Task UninstallService()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        await StopService();
        foreach (var config in Configs)
        {
            if (!config.Enabled)
                continue;

            await Nssm.UninstallService(ServicePrefix + config.Name);
        }
        await UpdateServiceStatus();
    }

    [RelayCommand]
    public async Task RestartService()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.Enabled)
                continue;

            await Nssm.RestartService(ServicePrefix + config.Name);
        }
        await UpdateServiceStatus();
    }

    [RelayCommand]
    public async Task StopService()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.Enabled)
                continue;

            await Nssm.StopService(ServicePrefix + config.Name);
        }
        await UpdateServiceStatus();
    }

    [RelayCommand]
    public void EditConfig(string configName)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, configName + ".toml");
        if (!File.Exists(configFile))
            return;

        Process.Start(new ProcessStartInfo("explorer.exe", configFile) { UseShellExecute = true });
    }

    private static string GetRpcPortal(string configName)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, configName + ".toml");
        if (!File.Exists(configFile))
            return "";

        var toml = Toml.ReadFile(configFile);
        var rpcPortal = toml.TryGetValue("rpc_portal");
        if (rpcPortal is null)
            return "";

        return rpcPortal.Get<string>();
    }

    [RelayCommand]
    public async Task ShowPeers()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        Messages = "";

        foreach (var config in Configs)
        {
            if (config.Status != ServiceStatus.Running)
                continue;

            var rpcPortal = GetRpcPortal(config.Name);
            var args = rpcPortal == "" ? "" : $"-p {rpcPortal}";

            var peers = await Nssm.RunWithOutput(AppSettings.EasytierCliPath, $"{args} peer", Encoding.UTF8);
            Messages += $"{config.Name}:\n{peers}\n\n";
        }
    }

    [RelayCommand]
    public async Task ShowRoute()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        Messages = "";

        foreach (var config in Configs)
        {
            if (config.Status != ServiceStatus.Running)
                continue;

            var rpcPortal = GetRpcPortal(config.Name);
            var args = rpcPortal == "" ? "" : $"-p {rpcPortal}";

            var route = await Nssm.RunWithOutput(AppSettings.EasytierCliPath, $"{args} route", Encoding.UTF8);
            Messages += $"{config.Name}:\n{route}\n\n";
        }
    }

    [ObservableProperty]
    public partial string Messages { get; set; } = "";

    public async Task UpdateServiceStatus()
    {
        foreach (var config in Configs)
        {
            if (!config.Enabled)
                continue;

            config.Status = await Nssm.GetServiceStatus(ServicePrefix + config.Name);
        }
    }

    [RelayCommand]
    public async Task UpdateEasytier()
    {
        var hasUpdate = await EasytierUpdate.HasUpdate();

        Messages = $"Local version is {EasytierUpdate.LocalVersion}, remote version is {EasytierUpdate.RemoteVersion}";

        if (hasUpdate)
        {
            await ForceUpdateEasytier();
        }
        else
        {
            Messages += "\nNo update found.";
        }
    }

    private async Task ForceUpdateEasytier()
    {
        Messages += "\nUpdating easytier...";
        await StopService();
        await EasytierUpdate.Update();
        CheckHasEasytier();
        await RestartService();
        Messages += "\nUpdate completed.";
    }

    [RelayCommand]
    public async Task UpdateMe()
    {
        if (await AutoUpdate.HasUpdate())
        {
            ForceUpdateMe();
        }
        else
        {
            Messages = "\nNo update found.";
        }
    }

    private void ForceUpdateMe()
    {
        Messages = "\nUpdating Me...";
        AutoUpdate.Update();
    }

    [ObservableProperty]
    public partial bool HasEasytier { get; set; } = true;

    private void CheckHasEasytier()
    {
        HasEasytier = File.Exists(AppSettings.EasytierCorePath) && File.Exists(AppSettings.EasytierCliPath);
    }

    [RelayCommand]
    public void SwitchTheme()
    {
        if (Application.Current is null)
            return;

        // If the theme is default, switch to the opposite theme
        var requestedTheme = Application.Current.RequestedThemeVariant;
        if (requestedTheme == ThemeVariant.Default)
        {
            var actualTheme = Application.Current.ActualThemeVariant;
            Application.Current.RequestedThemeVariant =
                actualTheme == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        }
        else // If the theme is not default, switch to default
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
        }
    }

    private const string RunKeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "JeekEasytierManager";

    [ObservableProperty]
    public partial bool StartOnBoot { get; set; } =
        (string?)Registry.GetValue(RunKeyPath, RunValueName, "") == AppSettings.ExePath;

    partial void OnStartOnBootChanged(bool value)
    {
        try
        {

            if (value)
            {
                // Add to registry startup
                RegistryHelper.SetValue(RunKeyPath, RunValueName, AppSettings.ExePath);
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
                if (await EasytierUpdate.HasUpdate())
                {
                    Messages = $"Auto update Easytier: New version {EasytierUpdate.RemoteVersion} available, updating...";
                    await ForceUpdateEasytier();
                }
            }

            if (AutoUpdateMe)
            {
                if (await AutoUpdate.HasUpdate())
                {
                    Messages = "Auto update me: New version available, updating...";
                    ForceUpdateMe();
                }
            }
        }
        catch (Exception ex)
        {
            Messages = $"Auto update error: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _autoUpdateTimer?.Stop();
    }
}
