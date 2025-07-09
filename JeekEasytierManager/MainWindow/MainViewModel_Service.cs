using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private async Task LoadInstalledServices(List<ConfigInfo>? configs = null)
    {
        var configsToUpdate = configs ?? [.. Configs];

        // Get installed services
        var output = await Executor.RunWithOutput("sc", "query type= service state= all");
        var lines = output.Split('\n');
        var installedServices = new HashSet<string>();

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("SERVICE_NAME:"))
            {
                var serviceName = line.Split(':')[1].Trim();
                if (serviceName.StartsWith(ServicePrefix))
                {
                    var configName = serviceName[ServicePrefix.Length..];
                    installedServices.Add(configName);
                }
            }
        }

        foreach (var config in configsToUpdate)
        {
            config.IsInstalled = installedServices.Contains(config.Name);
        }
    }

    private const string ServicePrefix = "Easytier ";

    [RelayCommand]
    public async Task InstallSelectedServices()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        await AddEasytierToFirewall();

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            await InstallService(config);
        }

        await LoadInstalledServices();

        foreach (var config in Configs)
        {
            if (!config.IsSelected || !config.IsInstalled)
                continue;

            await RestartService(config);
            await UpdateServiceStatus(config);
        }
    }

    [RelayCommand]
    public async Task InstallSingleService(ConfigInfo config)
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        await AddEasytierToFirewall();

        await InstallService(config);

        await LoadInstalledServices();
        await RestartService(config);
        await UpdateServiceStatus(config);
    }

    private async Task InstallService(ConfigInfo config)
    {
        var configPath = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (!await Nssm.InstallService(ServicePrefix + config.Name, AppSettings.EasytierCorePath, $"-c \"{configPath}\""))
        {
            Messages = $"Failed to install service {ServicePrefix + config.Name}\n{Nssm.LastError}";
            return;
        }

        config.IsInstalled = true;
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
    public async Task UninstallSelectedServices()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            await UninstallService(config);
        }

        await LoadInstalledServices();

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            await UpdateServiceStatus(config);
        }
    }

    [RelayCommand]
    public async Task UninstallSingleService(ConfigInfo config)
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        await UninstallService(config);

        await LoadInstalledServices();
        await UpdateServiceStatus(config);
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
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.IsSelected || !config.IsInstalled)
                continue;

            await RestartService(config);
            await UpdateServiceStatus(config);
        }
    }

    [RelayCommand]
    public async Task RestartSingleService(ConfigInfo config)
    {
        await RestartService(config);
        await UpdateServiceStatus(config);
    }

    public async Task RestartService(ConfigInfo config)
    {
        if (!config.IsInstalled)
            return;

        if (!await Nssm.RestartService(ServicePrefix + config.Name))
        {
            Messages = $"Failed to restart service {ServicePrefix + config.Name}\n{Nssm.LastOutput}\n{Nssm.LastError}";
            return;
        }

        // Wait 3 seconds to make sure the tun device is ready
        DispatcherTimer.RunOnce(async () =>
        {
            // Since the interface seems to be private, but not really private, we need to set it to private again.
            var configData = config.GetConfig();
            if (configData.Flags?.NoTun ?? false)
                return;

            if (string.IsNullOrEmpty(configData.Flags?.DevName))
                return;

            await Executor.RunAndWait("powershell.exe", $"-ex bypass -command Set-NetConnectionProfile -InterfaceAlias \"{configData.Flags.DevName}\" -NetworkCategory Private", false, true);
        }, TimeSpan.FromSeconds(3));
    }

    [RelayCommand]
    public async Task StopSelectedServices()
    {
        if (!HasEasytier)
        {
            Messages = "Easytier is not installed";
            return;
        }

        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            await StopService(config);
            await UpdateServiceStatus(config);
        }
    }

    [RelayCommand]
    public async Task StopSingleService(ConfigInfo config)
    {
        await StopService(config);
        await UpdateServiceStatus(config);
    }

    public async Task StopService(ConfigInfo config)
    {
        if (config.Status != ServiceStatus.Running)
            return;

        if (!await Nssm.StopService(ServicePrefix + config.Name))
        {
            Messages = $"Failed to stop service {ServicePrefix + config.Name}\n{Nssm.LastOutput}\n{Nssm.LastError}";
            return;
        }
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
                await UpdateServiceStatus(config);
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
            await UpdateServiceStatus(config);
        }

        _stoppedConfigNames.Clear();
    }

    [ObservableProperty]
    public partial bool HasRunningService { get; set; }

    public async Task UpdateServiceStatus(ConfigInfo config)
    {
        if (config.IsInstalled)
        {
            config.Status = await Nssm.GetServiceStatus(ServicePrefix + config.Name);
        }
        else
        {
            config.Status = ServiceStatus.None;
        }
    }

    public async Task UpdateAllServicesStatus(List<ConfigInfo>? configs = null)
    {
        var configsToUpdate = configs ?? [.. Configs];

        foreach (var config in configsToUpdate)
        {
            await UpdateServiceStatus(config);
        }

        HasRunningService = configsToUpdate.Any(c => c.Status == ServiceStatus.Running);
    }

}