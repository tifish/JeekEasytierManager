using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;
using Tomlyn;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private static string GetRpcSocket(string configName)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, configName + ".toml");
        if (!File.Exists(configFile))
            return "";

        const string defaultIp = "127.0.0.1";
        const string defaultPort = "15888";
        const string defaultRpcSocket = $"{defaultIp}:{defaultPort}";

        var toml = Toml.Parse(File.ReadAllText(configFile)).ToModel();
        if (!toml.TryGetValue("rpc_portal", out var rpcPortalValue))
            return defaultRpcSocket;

        var rpcPortal = ((string)rpcPortalValue).Trim();

        if (rpcPortal == "")
            return defaultRpcSocket;

        var parts = rpcPortal.Split(':');
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
                return rpcPortal;
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


}