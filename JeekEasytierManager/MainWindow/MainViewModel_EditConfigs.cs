using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomlyn;
using Tomlyn.Model;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    public partial string InstanceName { get; set; }
    [ObservableProperty]
    public partial string InstanceId { get; set; }

    [ObservableProperty]
    public partial string NetworkName { get; set; }
    [ObservableProperty]
    public partial string NetworkSecret { get; set; }

    [ObservableProperty]
    public partial bool EditIpAddress { get; set; }
    [ObservableProperty]
    public partial bool Dhcp { get; set; }
    [ObservableProperty]
    public partial string Ipv4 { get; set; }

    [ObservableProperty]
    public partial bool EditPeers { get; set; }
    [ObservableProperty]
    public partial string Peers { get; set; }
    [ObservableProperty]
    public partial bool EditListeners { get; set; }
    [ObservableProperty]
    public partial string Listeners { get; set; }

    [ObservableProperty]
    public partial bool EditRpcPortal { get; set; }
    [ObservableProperty]
    public partial string RpcPortal { get; set; }

    [ObservableProperty]
    public partial bool EditProxyNetworks { get; set; }
    [ObservableProperty]
    public partial string ProxyNetworks { get; set; }

    [ObservableProperty]
    public partial bool LatencyFirst { get; set; }

    [ObservableProperty]
    public partial bool UseSmoltcp { get; set; }

    [ObservableProperty]
    public partial bool EnableKcpProxy { get; set; }

    [ObservableProperty]
    public partial bool DisableKcpInput { get; set; }

    [ObservableProperty]
    public partial bool EnableQuicProxy { get; set; }

    [ObservableProperty]
    public partial bool DisableQuicInput { get; set; }

    [ObservableProperty]
    public partial bool DisableP2p { get; set; }

    [ObservableProperty]
    public partial bool NotBindDevice { get; set; } = true;

    [ObservableProperty]
    public partial bool NoTun { get; set; }

    [ObservableProperty]
    public partial bool EnableExitNode { get; set; }

    [ObservableProperty]
    public partial bool RelayAllPeerRpc { get; set; }

    [ObservableProperty]
    public partial bool MultiThread { get; set; } = true;

    [ObservableProperty]
    public partial bool ProxyForwardBySystem { get; set; }

    [ObservableProperty]
    public partial bool DisableEncryption { get; set; }

    [ObservableProperty]
    public partial bool DisableUdpHolePunching { get; set; }

    [ObservableProperty]
    public partial bool AcceptDns { get; set; }

    [ObservableProperty]
    public partial bool PrivateMode { get; set; }

    public void LoadConfig(string configName)
    {
        var configPath = Path.Join(AppSettings.ConfigDirectory, configName + ".toml");
        var configData = Toml.Parse(File.ReadAllText(configPath)).ToModel();

        InstanceName = configName;
        InstanceId = configData.Get("instance_id", "");

        if (configData.TryGetValue("network_identity", out var networkIdentityObj))
        {
            var networkIdentity = (TomlTable)networkIdentityObj;
            NetworkName = networkIdentity.Get("network_name", "");
            NetworkSecret = networkIdentity.Get("network_secret", "");
        }
        else
        {
            NetworkName = "";
            NetworkSecret = "";
        }

        Dhcp = configData.Get("dhcp", false);
        Ipv4 = configData.Get("ipv4", "");

        if (configData.TryGetValue("peer", out var peers))
        {
            var peersArray = (TomlTableArray)peers;
            Peers = string.Join("\n", peersArray.Select(peer => (string)peer["uri"]));
        }
        else
        {
            Peers = "";
        }

        if (configData.TryGetValue("listeners", out var listeners))
        {
            var listenersArray = (TomlArray)listeners;
            Listeners = string.Join("\n", listenersArray.Select(listener => (string)listener!));
        }
        else
        {
            Listeners = "";
        }

        RpcPortal = configData.Get("rpc_portal", "");

        if (configData.TryGetValue("proxy_network", out var proxyNetworks))
        {
            var proxyNetworksArray = (TomlTableArray)proxyNetworks;
            ProxyNetworks = string.Join("\n", proxyNetworksArray.Select(proxyNetwork => (string)proxyNetwork["cidr"]));
        }
        else
        {
            ProxyNetworks = "";
        }

        if (configData.TryGetValue("flags", out var flagsObj))
        {
            var flags = (TomlTable)flagsObj;

            LatencyFirst = flags.Get("latency_first", false);
            UseSmoltcp = flags.Get("use_smoltcp", false);
            EnableKcpProxy = flags.Get("enable_kcp_proxy", false);
            DisableKcpInput = flags.Get("disable_kcp_input", false);
            EnableQuicProxy = flags.Get("enable_quic_proxy", false);
            DisableQuicInput = flags.Get("disable_quic_input", false);
            DisableP2p = flags.Get("disable_p2p", false);
            NotBindDevice = !flags.Get("bind_device", true);
            NoTun = flags.Get("no_tun", false);
            EnableExitNode = flags.Get("enable_exit_node", false);
            RelayAllPeerRpc = flags.Get("relay_all_peer_rpc", false);
            MultiThread = !flags.Get("multi_thread", true);
            ProxyForwardBySystem = flags.Get("proxy_forward_by_system", false);
            DisableEncryption = !flags.Get("enable_encryption", false);
            DisableUdpHolePunching = flags.Get("disable_udp_hole_punching", false);
            AcceptDns = flags.Get("accept_dns", false);
            PrivateMode = flags.Get("private_mode", false);
        }
        else
        {
            LatencyFirst = false;
            UseSmoltcp = false;
            EnableKcpProxy = false;
            DisableKcpInput = false;
            EnableQuicProxy = false;
            DisableQuicInput = false;
            DisableP2p = false;
            NotBindDevice = true;
            NoTun = false;
            EnableExitNode = false;
            RelayAllPeerRpc = false;
            MultiThread = true;
            ProxyForwardBySystem = false;
            DisableEncryption = false;
            DisableUdpHolePunching = false;
            AcceptDns = false;
            PrivateMode = false;
        }
    }

    [RelayCommand]
    public void SaveEditConfigs()
    {
        foreach (var config in Configs.ToArray())
        {
            if (!config.IsSelected)
                continue;

            var configPath = Path.Join(AppSettings.ConfigDirectory, config.Name + ".toml");
            var configData = Toml.Parse(File.ReadAllText(configPath)).ToModel();

            configData["instance_name"] = config.Name;
            if (!configData.TryGetValue("instance_id", out var instanceId)) // Add new instance_id if not exist
                configData["instance_id"] = Guid.NewGuid().ToString();

            configData.Set("network_identity", new TomlTable
            {
                ["network_name"] = NetworkName,
                ["network_secret"] = NetworkSecret
            });

            if (EditIpAddress)
            {
                configData["dhcp"] = Dhcp;
                configData["ipv4"] = Ipv4;
            }

            if (EditPeers)
            {
                var peersArray = Peers.Split('\n').Select(peer => peer.Trim()).Where(peer => !string.IsNullOrEmpty(peer)).ToArray();
                var tomlArray = new TomlTableArray();
                foreach (var peer in peersArray)
                {
                    tomlArray.Add(new TomlTable { ["uri"] = peer, });
                }

                configData["peer"] = tomlArray;
            }

            if (EditListeners)
            {
                var listenersArray = Listeners.Split('\n').Select(listener => listener.Trim()).Where(listener => !string.IsNullOrEmpty(listener)).ToArray();
                var tomlArray = new TomlArray(listenersArray.Length);
                foreach (var listener in listenersArray)
                    tomlArray.Add(listener);
                configData["listeners"] = tomlArray;
            }

            if (EditRpcPortal)
                configData["rpc_portal"] = RpcPortal;

            if (EditProxyNetworks)
            {
                var proxyNetworksArray = ProxyNetworks.Split('\n').Select(proxyNetwork => proxyNetwork.Trim()).Where(proxyNetwork => !string.IsNullOrEmpty(proxyNetwork)).ToArray();
                var tomlArray = new TomlTableArray();
                foreach (var proxyNetwork in proxyNetworksArray)
                {
                    tomlArray.Add(new TomlTable { ["cidr"] = proxyNetwork });
                }
                configData["proxy_network"] = tomlArray;
            }

            var flags = new TomlTable();
            configData["flags"] = flags;

            flags["dev_name"] = config.Name;

            SetFlag(flags, LatencyFirst, "latency_first", true);
            SetFlag(flags, UseSmoltcp, "use_smoltcp", true);
            SetFlag(flags, EnableKcpProxy, "enable_kcp_proxy", true);
            SetFlag(flags, DisableKcpInput, "disable_kcp_input", true);
            SetFlag(flags, EnableQuicProxy, "enable_quic_proxy", true);
            SetFlag(flags, DisableQuicInput, "disable_quic_input", true);
            SetFlag(flags, DisableP2p, "disable_p2p", true);
            SetFlag(flags, !NotBindDevice, "bind_device", true);
            SetFlag(flags, NoTun, "no_tun", true);
            SetFlag(flags, EnableExitNode, "enable_exit_node", true);
            SetFlag(flags, RelayAllPeerRpc, "relay_all_peer_rpc", true);
            SetFlag(flags, !MultiThread, "multi_thread", false);
            SetFlag(flags, ProxyForwardBySystem, "proxy_forward_by_system", true);
            SetFlag(flags, DisableEncryption, "enable_encryption", false);
            SetFlag(flags, DisableUdpHolePunching, "disable_udp_hole_punching", true);
            SetFlag(flags, AcceptDns, "accept_dns", true);
            SetFlag(flags, PrivateMode, "private_mode", true);

            File.WriteAllText(configPath, Toml.FromModel(configData));
        }

        CloseEditConfigs();
    }

    private static void SetFlag(TomlTable flags, bool condition, string key, bool value)
    {
        if (condition)
            flags[key] = value;
        else
            flags.Remove(key);
    }

    [RelayCommand]
    public void CloseEditConfigs()
    {
        IsEditingConfigs = false;
        MainGrid.RowDefinitions[0].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
        MainGrid.RowDefinitions[1].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
    }
}