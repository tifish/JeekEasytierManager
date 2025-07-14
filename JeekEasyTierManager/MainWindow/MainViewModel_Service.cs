using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private void LoadInstalledServices(List<ConfigInfo>? configs = null)
    {
        var configsToUpdate = configs ?? [.. Configs];

        // Get installed services
        var easyTierServices = ServiceController.GetServices().Where(s => s.ServiceName.StartsWith(ServicePrefix));

        foreach (var config in configsToUpdate)
        {
            config.Service = easyTierServices.FirstOrDefault(s => s.ServiceName == ServicePrefix + config.Name);
            config.IsInstalled = config.Service != null;
        }
    }

    private const string ServicePrefix = "EasyTier ";

    [RelayCommand]
    public async Task InstallSelectedServices()
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        await AddEasyTierToFirewall();

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            await InstallService(config);
        }

        LoadInstalledServices();

        foreach (var config in Configs)
        {
            if (!config.IsSelected || !config.IsInstalled)
                continue;

            await RestartService(config);
            UpdateServiceStatus(config);
        }

        await ShowInfo();
    }

    [RelayCommand]
    public async Task InstallSingleService(ConfigInfo config)
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        await AddEasyTierToFirewall();

        await InstallService(config);

        LoadInstalledServices();
        await RestartService(config);
        UpdateServiceStatus(config);
        await ShowInfo();
    }

    private async Task InstallService(ConfigInfo config)
    {
        var configPath = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (!await Nssm.InstallService(ServicePrefix + config.Name, AppSettings.EasyTierCorePath, $"-c \"{configPath}\""))
        {
            Messages = $"Failed to install service {ServicePrefix + config.Name}\n{Nssm.LastError}";
            return;
        }

        config.IsInstalled = true;
    }

    private async Task AddEasyTierToFirewall()
    {
        // Delete existing firewall rule
        var deleteRuleArgs = $"""advfirewall firewall delete rule name="EasyTier Core" """;
        await Executor.RunAndWait("netsh", deleteRuleArgs, false, true);

        // Add new firewall rule
        var firewallArgs = $"""advfirewall firewall add rule name="EasyTier Core" dir=in action=allow program="{AppSettings.EasyTierCorePath}" enable=yes""";
        await Executor.RunAndWait("netsh", firewallArgs, false, true);
    }

    [RelayCommand]
    public async Task UninstallSelectedServices()
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            await UninstallService(config);
        }

        LoadInstalledServices();

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            UpdateServiceStatus(config);
        }

        await ShowInfo();
    }

    [RelayCommand]
    public async Task UninstallSingleService(ConfigInfo config)
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        await UninstallService(config);

        LoadInstalledServices();
        UpdateServiceStatus(config);

        await ShowInfo();
    }

    private async Task<bool> UninstallService(ConfigInfo config)
    {
        if (!config.IsInstalled)
            return true;

        await StopService(config);

        if (await Nssm.UninstallService(ServicePrefix + config.Name))
        {
            config.IsInstalled = false;
            return true;
        }
        else
        {
            Messages = $"Failed to uninstall service {ServicePrefix + config.Name}\n{Nssm.LastOutput}\n{Nssm.LastError}";
            return false;
        }
    }

    [RelayCommand]
    public async Task RestartSelectedServices()
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.IsSelected || !config.IsInstalled)
                continue;

            await RestartService(config);
            UpdateServiceStatus(config);
        }

        await ShowInfo();
    }

    [RelayCommand]
    public async Task RestartSingleService(ConfigInfo config)
    {
        await RestartService(config);
        UpdateServiceStatus(config);
        await ShowInfo();
    }

    public async Task RestartService(ConfigInfo config)
    {
        if (config.Service == null)
            return;

        await config.Service.StopAsync();
        await config.Service.StartAsync();

        // Wait 3 seconds to make sure the tun device is ready
        DispatcherTimer.RunOnce(async () =>
        {
            // Since the interface seems to be private, but not really private, we need to set it to private again.
            var configData = config.GetConfig();
            if (configData == null)
                return;
            if (configData.Flags?.NoTun ?? false)
                return;

            if (string.IsNullOrEmpty(configData!.Flags?.DevName))
                return;

            await Executor.RunAndWait("powershell.exe", $"-ex bypass -command Set-NetConnectionProfile -InterfaceAlias \"{configData.Flags.DevName}\" -NetworkCategory Private", false, true);
        }, TimeSpan.FromSeconds(3));
    }

    [RelayCommand]
    public async Task StopSelectedServices()
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            await StopService(config);
            UpdateServiceStatus(config);
        }

        await ShowInfo();
    }

    [RelayCommand]
    public async Task StopSingleService(ConfigInfo config)
    {
        await StopService(config);
        UpdateServiceStatus(config);
        await ShowInfo();
    }

    public async Task StopService(ConfigInfo config)
    {
        if (config.Service == null)
            return;

        await config.Service.StopAsync();
    }

    private readonly List<string> _stoppedConfigNames = [];

    private async Task StopAllServices()
    {
        _stoppedConfigNames.Clear();

        foreach (var config in Configs)
        {
            if (config.Status == ServiceStatus.Running)
            {
                await StopService(config);
                UpdateServiceStatus(config);
                _stoppedConfigNames.Add(config.Name);
            }
        }
    }

    private async Task RestoreAllServices()
    {
        foreach (var configName in _stoppedConfigNames)
        {
            var config = Configs.FirstOrDefault(c => c.Name == configName);
            if (config == null)
                continue;

            await RestartService(config);
            UpdateServiceStatus(config);
        }

        _stoppedConfigNames.Clear();
    }

    [ObservableProperty]
    public partial bool HasRunningService { get; set; }

    public void UpdateServiceStatus(ConfigInfo config)
    {
        if (config.Service != null)
        {
            config.Service.Refresh();

            config.Status = (ServiceStatus)config.Service.Status;
        }
        else
        {
            config.Status = ServiceStatus.None;
        }

        HasRunningService = Configs.Any(c => c.Status == ServiceStatus.Running);
    }

    public void UpdateAllServicesStatus(List<ConfigInfo>? configs = null)
    {
        var configsToUpdate = configs ?? [.. Configs];

        foreach (var config in configsToUpdate)
        {
            UpdateServiceStatus(config);
        }
    }

}