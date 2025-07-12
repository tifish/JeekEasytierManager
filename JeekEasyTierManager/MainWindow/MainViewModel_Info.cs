using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;
using Nett;

namespace JeekEasyTierManager;

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

        var toml = Toml.ReadFile(configFile);
        var rpcPortal = toml.Get("rpc_portal", "");
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

    private bool _showPeersOrRoute = true;

    [RelayCommand]
    public async Task ShowPeers()
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        var messages = new StringBuilder();

        foreach (var config in Configs)
        {
            if (config.Status != ServiceStatus.Running)
                continue;

            var rpcSocket = GetRpcSocket(config.Name);
            var peers = await Executor.RunWithOutput(AppSettings.EasyTierCliPath, $"-p {rpcSocket} peer", Encoding.UTF8);
            messages.AppendLine($"{config.Name}:");
            messages.AppendLine(peers);
            messages.AppendLine();
        }

        Messages = messages.ToString();

        _showPeersOrRoute = true;
    }

    [RelayCommand]
    public async Task ShowRoute()
    {
        if (!HasEasyTier)
        {
            Messages = "EasyTier is not installed";
            return;
        }

        var messages = new StringBuilder();

        foreach (var config in Configs)
        {
            if (config.Status != ServiceStatus.Running)
                continue;

            var rpcSocket = GetRpcSocket(config.Name);
            var route = await Executor.RunWithOutput(AppSettings.EasyTierCliPath, $"-p {rpcSocket} route", Encoding.UTF8);
            messages.AppendLine($"{config.Name}:");
            messages.AppendLine(route);
            messages.AppendLine();
        }

        Messages = messages.ToString();

        _showPeersOrRoute = false;
    }

    public async Task ShowInfo()
    {
        if (_showPeersOrRoute)
            await ShowPeers();
        else
            await ShowRoute();
    }

    [ObservableProperty]
    public partial string Messages { get; set; } = "";

}