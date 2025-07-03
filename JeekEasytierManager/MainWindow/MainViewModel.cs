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
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Nett;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public static MainViewModel Instance { get; private set; } = new();

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
        var output = await Executor.RunWithOutput("sc", "query state= all");
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

        await AddEasytierToFirewall();

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

    private async Task AddEasytierToFirewall()
    {
        // Delete existing firewall rule
        var deleteRuleArgs = $"""advfirewall firewall delete rule name="Easytier Core" """;
        await Executor.RunAndWait("netsh", deleteRuleArgs, false, true);

        // Add new firewall rule
        var firewallArgs = $"""advfirewall firewall add rule name="Easytier Core" dir=in action=allow program="{AppSettings.EasytierCorePath}" enable=yes""";
        await Executor.RunAndWait("netsh", firewallArgs, false, true);
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
    public void EditConfig(ConfigInfo config)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (!File.Exists(configFile))
            return;

        Process.Start(new ProcessStartInfo("explorer.exe", configFile) { UseShellExecute = true });
    }

    [ObservableProperty]
    public partial bool RenameConfigDialogIsOpen { get; set; } = false;

    [ObservableProperty]
    public partial string RenameConfigDialogText { get; set; } = "";

    private ConfigInfo? _renameConfigDialogOldConfig = null;

    [RelayCommand]
    public void RenameConfig(ConfigInfo config)
    {
        RenameConfigDialogIsOpen = true;
        RenameConfigDialogText = config.Name;
        _renameConfigDialogOldConfig = config;
    }

    [RelayCommand]
    public void RenameConfigDialogCancel()
    {
        RenameConfigDialogIsOpen = false;
    }

    [RelayCommand]
    public void RenameConfigDialogSave()
    {
        RenameConfigDialogIsOpen = false;

        if (_renameConfigDialogOldConfig is null)
            return;

        var newName = RenameConfigDialogText;

        if (string.IsNullOrWhiteSpace(newName) || newName == _renameConfigDialogOldConfig.Name)
            return;

        var oldConfigFile = Path.Combine(AppSettings.ConfigDirectory, _renameConfigDialogOldConfig.Name + ".toml");
        var newConfigFile = Path.Combine(AppSettings.ConfigDirectory, newName + ".toml");

        if (File.Exists(newConfigFile))
        {
            Messages = $"Config file '{newName}.toml' already exists.";
            return;
        }

        try
        {
            File.Move(oldConfigFile, newConfigFile);

            _renameConfigDialogOldConfig.Name = newName;
            _renameConfigDialogOldConfig = null;
        }
        catch (Exception ex)
        {
            Messages = $"Failed to rename config: {ex.Message}";
        }
    }

    [ObservableProperty]
    public partial bool AddConfigDialogIsOpen { get; set; } = false;

    [ObservableProperty]
    public partial string AddConfigDialogText { get; set; } = "";

    [RelayCommand]
    public void AddConfigDialogCancel()
    {
        AddConfigDialogIsOpen = false;
    }

    [RelayCommand]
    public void AddConfigDialogAdd()
    {
        AddConfigDialogIsOpen = false;

        var newName = AddConfigDialogText;
        if (string.IsNullOrWhiteSpace(newName))
            return;

        var configFile = Path.Combine(AppSettings.ConfigDirectory, newName + ".toml");
        if (File.Exists(configFile))
            return;

        File.Create(configFile).Close();
        Configs.Add(new ConfigInfo { Name = newName });
    }

    [RelayCommand]
    public async Task DeleteConfig(ConfigInfo config)
    {
        var result = await MessageBoxManager.GetMessageBoxStandard("Delete Config", "Are you sure you want to delete this config?", ButtonEnum.YesNo).ShowAsync();
        if (result != ButtonResult.Yes)
            return;

        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (File.Exists(configFile))
            File.Delete(configFile);

        Configs.Remove(config);
    }

    private static string GetRpcSocket(string configName)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, configName + ".toml");
        if (!File.Exists(configFile))
            return "";

        const string defaultIp = "127.0.0.1";
        const string defaultPort = "15888";
        const string defaultRpcSocket = $"{defaultIp}:{defaultPort}";

        var toml = Toml.ReadFile(configFile);
        var rpcPortal = toml.TryGetValue("rpc_portal");
        if (rpcPortal is null)
            return defaultRpcSocket;

        var rpcPortalString = rpcPortal.Get<string>().Trim();

        if (rpcPortalString == "")
            return defaultRpcSocket;

        var parts = rpcPortalString.Split(':');
        if (parts.Length == 1)
        {
            if (parts[0] == "0")
                return defaultRpcSocket;
            else
                return $"{defaultIp}:{parts[0]}";
        }
        else if (parts.Length == 2)
        {
            if (parts[0] == "0.0.0.0")
                return $"{defaultIp}:{parts[1]}";
            else
                return rpcPortalString;
        }

        return defaultRpcSocket;
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

            var rpcSocket = GetRpcSocket(config.Name);
            var peers = await Executor.RunWithOutput(AppSettings.EasytierCliPath, $"-p {rpcSocket} peer", Encoding.UTF8);
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

            var rpcSocket = GetRpcSocket(config.Name);
            var route = await Executor.RunWithOutput(AppSettings.EasytierCliPath, $"-p {rpcSocket} route", Encoding.UTF8);
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
    private static readonly string RunValue = $"\"{AppSettings.ExePath}\" /hide";

    [ObservableProperty]
    public partial bool StartOnBoot { get; set; } =
        (string?)Registry.GetValue(RunKeyPath, RunValueName, "") == RunValue;

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

        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    public void EditSelectedConfig()
    {
    }

    [RelayCommand]
    public void AddConfig()
    {
        AddConfigDialogIsOpen = true;
    }
}
