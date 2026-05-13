using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;
using Json.Easy;
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
        _showPeersOrRoute = true;
        IsShowingPeers = true;
        IsShowingRoutes = false;

        if (!HasEasyTier)
        {
            PeerInfoRows = [];
            AddMessage("EasyTier is not installed");
            return;
        }

        var peerInfoRows = new ObservableCollection<PeerInfoRow>();
        var hasRunningService = false;

        foreach (var config in Configs.ToArray()) // Solve problem of modifying collection while iterating
        {
            if (config.Status != ServiceStatus.Running)
                continue;

            hasRunningService = true;

            try
            {
                var rpcSocket = GetRpcSocket(config.Name);
                var peersJson = await Executor.RunWithOutput(
                    AppSettings.EasyTierCliPath,
                    $"-p {rpcSocket} -o json peer",
                    Encoding.UTF8
                );
                var peers = JsonFile.FromJson<List<PeerInfo>>(peersJson) ?? [];
                foreach (var peer in peers)
                    peerInfoRows.Add(new PeerInfoRow(config.Name, peer));
            }
            catch (Exception ex)
            {
                AddMessage($"Failed to load peers from {config.Name}: {ex.Message}");
            }
        }

        if (!hasRunningService)
            AddMessage("No running EasyTier services found");

        PeerInfoRows = peerInfoRows;
    }

    [RelayCommand]
    public async Task ShowRoute()
    {
        _showPeersOrRoute = false;
        IsShowingPeers = false;
        IsShowingRoutes = true;

        if (!HasEasyTier)
        {
            RouteInfoRows = [];
            AddMessage("EasyTier is not installed");
            return;
        }

        var routeInfoRows = new ObservableCollection<RouteInfoRow>();
        var hasRunningService = false;

        foreach (var config in Configs.ToArray()) // Solve problem of modifying collection while iterating
        {
            if (config.Status != ServiceStatus.Running)
                continue;

            hasRunningService = true;

            try
            {
                var rpcSocket = GetRpcSocket(config.Name);
                var routeJson = await Executor.RunWithOutput(
                    AppSettings.EasyTierCliPath,
                    $"-p {rpcSocket} -o json route",
                    Encoding.UTF8
                );
                var routes = JsonFile.FromJson<List<RouteInfo>>(routeJson) ?? [];
                foreach (var route in routes)
                    routeInfoRows.Add(new RouteInfoRow(config.Name, route));
            }
            catch (Exception ex)
            {
                AddMessage($"Failed to load routes from {config.Name}: {ex.Message}");
            }
        }

        if (!hasRunningService)
            AddMessage("No running EasyTier services found");

        RouteInfoRows = routeInfoRows;
    }

    [ObservableProperty]
    public partial ObservableCollection<PeerInfoRow> PeerInfoRows { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<RouteInfoRow> RouteInfoRows { get; set; } = [];

    [ObservableProperty]
    public partial bool IsShowingPeers { get; set; } = true;

    [ObservableProperty]
    public partial bool IsShowingRoutes { get; set; } = false;

    public async Task ShowInfo()
    {
        if (_showPeersOrRoute)
            await ShowPeers();
        else
            await ShowRoute();
    }

    private readonly StringBuilder _messagesBuilder = new();

    public string Messages => _messagesBuilder.ToString();

    public void AddMessage(string message)
    {
        _messagesBuilder.AppendLine(message);
        OnPropertyChanged(nameof(Messages));
    }

    public void ClearMessages()
    {
        if (_messagesBuilder.Length == 0)
            return;

        _messagesBuilder.Clear();
        OnPropertyChanged(nameof(Messages));
    }

    [ObservableProperty]
    public partial double DownloadProgress { get; set; } = 0;

    [ObservableProperty]
    public partial string DownloadStatus { get; set; } = "";

    [ObservableProperty]
    public partial bool IsDownloading { get; set; } = false;
}

public class PeerInfoRow : PeerInfo
{
    public PeerInfoRow(string configName, PeerInfo peer)
    {
        ConfigName = configName;
        Cidr = peer.Cidr;
        Ipv4 = peer.Ipv4;
        Hostname = peer.Hostname;
        Cost = peer.Cost;
        LatMs = peer.LatMs;
        LossRate = peer.LossRate;
        RxBytes = peer.RxBytes;
        TxBytes = peer.TxBytes;
        TunnelProto = peer.TunnelProto;
        NatType = peer.NatType;
        Id = peer.Id;
        Version = peer.Version;
    }

    public string ConfigName { get; set; } = "";
}

public class RouteInfoRow : RouteInfo
{
    public RouteInfoRow(string configName, RouteInfo route)
    {
        ConfigName = configName;
        Ipv4 = route.Ipv4;
        Hostname = route.Hostname;
        ProxyCidrs = route.ProxyCidrs;
        NextHopIpv4 = route.NextHopIpv4;
        NextHopHostname = route.NextHopHostname;
        NextHopLat = route.NextHopLat;
        PathLen = route.PathLen;
        PathLatency = route.PathLatency;
        NextHopIpv4LatFirst = route.NextHopIpv4LatFirst;
        NextHopHostnameLatFirst = route.NextHopHostnameLatFirst;
        PathLenLatFirst = route.PathLenLatFirst;
        PathLatencyLatFirst = route.PathLatencyLatFirst;
        Version = route.Version;
    }

    public string ConfigName { get; set; } = "";

    public string NextHop =>
        string.IsNullOrWhiteSpace(NextHopHostname)
            ? NextHopIpv4
            : $"{NextHopIpv4} ({NextHopHostname})";
}
