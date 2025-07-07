using System;
using System.Collections.Generic;
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
        if (configData.TryGetValue("instance_id", out var instanceId))
            InstanceId = (string)instanceId;

        if (configData.TryGetValue("network_identity", out var networkIdentityObj))
        {
            var networkIdentity = (TomlTable)networkIdentityObj;
            if (networkIdentity.TryGetValue("network_name", out var networkName))
                NetworkName = (string)networkName;
            if (networkIdentity.TryGetValue("network_secret", out var networkSecret))
                NetworkSecret = (string)networkSecret;
        }

        if (configData.TryGetValue("dhcp", out var dhcp))
            Dhcp = (bool)dhcp;
        if (configData.TryGetValue("ipv4", out var ipv4))
            Ipv4 = (string)ipv4;

        if (configData.TryGetValue("peer", out var peers))
        {
            var peersArray = (TomlTableArray)peers;
            Peers = string.Join("\n", peersArray.Select(peer => (string)peer["uri"]));
        }
        if (configData.TryGetValue("listeners", out var listeners))
        {
            var listenersArray = (TomlArray)listeners;
            Listeners = string.Join("\n", listenersArray.Select(listener => (string)listener!));
        }

        if (configData.TryGetValue("rpc_portal", out var rpcPortal))
            RpcPortal = (string)rpcPortal;

        if (configData.TryGetValue("proxy_network", out var proxyNetworks))
        {
            var proxyNetworksArray = (TomlTableArray)proxyNetworks;
            ProxyNetworks = string.Join("\n", proxyNetworksArray.Select(proxyNetwork => (string)proxyNetwork["cidr"]));
        }

        if (configData.TryGetValue("flags", out var flagsObj))
        {
            var flags = (TomlTable)flagsObj;
            if (flags.TryGetValue("latency_first", out var latencyFirst))
                LatencyFirst = (bool)latencyFirst;
            if (flags.TryGetValue("use_smoltcp", out var useSmoltcp))
                UseSmoltcp = (bool)useSmoltcp;
            if (flags.TryGetValue("enable_kcp_proxy", out var enableKcpProxy))
                EnableKcpProxy = (bool)enableKcpProxy;
            if (flags.TryGetValue("disable_kcp_input", out var disableKcpInput))
                DisableKcpInput = (bool)disableKcpInput;
            if (flags.TryGetValue("enable_quic_proxy", out var enableQuicProxy))
                EnableQuicProxy = (bool)enableQuicProxy;
            if (flags.TryGetValue("disable_quic_input", out var disableQuicInput))
                DisableQuicInput = (bool)disableQuicInput;
            if (flags.TryGetValue("disable_p2p", out var disableP2p))
                DisableP2p = (bool)disableP2p;
            if (flags.TryGetValue("bind_device", out var bindDevice))
                NotBindDevice = !(bool)bindDevice;
            if (flags.TryGetValue("no_tun", out var noTun))
                NoTun = (bool)noTun;
            if (flags.TryGetValue("enable_exit_node", out var enableExitNode))
                EnableExitNode = (bool)enableExitNode;
            if (flags.TryGetValue("relay_all_peer_rpc", out var relayAllPeerRpc))
                RelayAllPeerRpc = (bool)relayAllPeerRpc;
            if (flags.TryGetValue("multi_thread", out var multiThread))
                MultiThread = !(bool)multiThread;
            if (flags.TryGetValue("proxy_forward_by_system", out var proxyForwardBySystem))
                ProxyForwardBySystem = (bool)proxyForwardBySystem;
            if (flags.TryGetValue("enable_encryption", out var enableEncryption))
                DisableEncryption = !(bool)enableEncryption;
            if (flags.TryGetValue("disable_udp_hole_punching", out var disableUdpHolePunching))
                DisableUdpHolePunching = (bool)disableUdpHolePunching;
            if (flags.TryGetValue("accept_dns", out var acceptDns))
                AcceptDns = (bool)acceptDns;
            if (flags.TryGetValue("private_mode", out var privateMode))
                PrivateMode = (bool)privateMode;
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

            if (InstanceName != null) // instance_name is always config.Name
                configData["instance_name"] = InstanceName;
            if (!configData.TryGetValue("instance_id", out var instanceId)) // Add new instance_id if not exist
                configData["instance_id"] = Guid.NewGuid().ToString();

            configData.Remove("network_identity");
            configData.Add("network_identity", new TomlTable
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