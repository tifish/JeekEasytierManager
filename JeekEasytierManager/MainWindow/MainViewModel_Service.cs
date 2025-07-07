using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private async Task LoadEnabledServices()
    {
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
                        config.IsSelected = true;
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
            if (!config.IsSelected)
                continue;

            var configPath = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
            if (!await Nssm.InstallService(ServicePrefix + config.Name, AppSettings.EasytierCorePath, $"-c \"{configPath}\""))
            {
                Messages = $"Failed to install service {ServicePrefix + config.Name}\n{Nssm.LastError}";
                return;
            }
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
            if (!config.IsSelected)
                continue;

            if (!await Nssm.UninstallService(ServicePrefix + config.Name))
            {
                Messages = $"Failed to uninstall service {ServicePrefix + config.Name}\n{Nssm.LastOutput}\n{Nssm.LastError}";
                return;
            }
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
            if (!config.IsSelected)
                continue;

            if (!await Nssm.RestartService(ServicePrefix + config.Name))
            {
                Messages = $"Failed to restart service {ServicePrefix + config.Name}\n{Nssm.LastOutput}\n{Nssm.LastError}";
                return;
            }
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
            if (!config.IsSelected)
                continue;

            if (!await Nssm.StopService(ServicePrefix + config.Name))
            {
                Messages = $"Failed to stop service {ServicePrefix + config.Name}\n{Nssm.LastOutput}\n{Nssm.LastError}";
                return;
            }
        }
        await UpdateServiceStatus();
    }

    public async Task UpdateServiceStatus()
    {
        foreach (var config in Configs)
        {
            if (!config.IsSelected)
                continue;

            config.Status = await Nssm.GetServiceStatus(ServicePrefix + config.Name);
        }
    }

}