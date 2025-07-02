using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nett;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject
{
    public static MainViewModel Instance { get; private set; } = new();

    [ObservableProperty]
    public partial string Title { get; set; } = "Jeek Easytier Manager";

    [ObservableProperty]
    public partial ObservableCollection<ConfigInfo> Configs { get; set; } = [];

    public async Task Init()
    {
        await LoadConfigs();
        await UpdateServiceStatus();
        CheckHasEasytier();
        await ShowPeers();
    }

    private async Task LoadConfigs()
    {
        if (!Directory.Exists(Settings.ConfigDirectory))
            return;

        // 获取配置文件列表
        var configFiles = Directory.GetFiles(Settings.ConfigDirectory, "*.toml");
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

            var configPath = Path.Combine(Settings.ConfigDirectory, config.Name + ".toml");
            await Nssm.InstallService(ServicePrefix + config.Name, Settings.EasytierCorePath, $"-c \"{configPath}\"");
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
        var configFile = Path.Combine(Settings.ConfigDirectory, configName + ".toml");
        if (!File.Exists(configFile))
            return;

        Process.Start(new ProcessStartInfo("explorer.exe", configFile) { UseShellExecute = true });
    }

    private static string GetRpcPortal(string configName)
    {
        var configFile = Path.Combine(Settings.ConfigDirectory, configName + ".toml");
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

            var peers = await Nssm.RunWithOutput(Settings.EasytierCliPath, $"{args} peer", Encoding.UTF8);
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

            var route = await Nssm.RunWithOutput(Settings.EasytierCliPath, $"{args} route", Encoding.UTF8);
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
            Messages += "\nUpdating easytier...";
            await StopService();
            await EasytierUpdate.Update();
            CheckHasEasytier();
            await RestartService();
            Messages += "\nUpdate completed.";
        }
        else
        {
            Messages += "\nNo update found.";
        }
    }

    [RelayCommand]
    public async Task UpdateMe()
    {
        if (await AutoUpdate.HasUpdate())
        {
            Messages = "\nUpdating Me...";
            AutoUpdate.Update();
        }
        else
        {
            Messages = "\nNo update found.";
        }
    }

    [ObservableProperty]
    public partial bool HasEasytier { get; set; } = true;

    private void CheckHasEasytier()
    {
        HasEasytier = File.Exists(Settings.EasytierCorePath) && File.Exists(Settings.EasytierCliPath);
    }
}

