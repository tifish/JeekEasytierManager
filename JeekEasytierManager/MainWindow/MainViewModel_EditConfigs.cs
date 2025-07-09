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
    public partial string HostName { get; set; } = "";

    [ObservableProperty]
    public partial string NetworkName { get; set; } = "";
    [ObservableProperty]
    public partial string NetworkSecret { get; set; } = "";

    [ObservableProperty]
    public partial bool EditIpAddress { get; set; }
    [ObservableProperty]
    public partial bool Dhcp { get; set; }
    [ObservableProperty]
    public partial string Ipv4 { get; set; } = "";

    [ObservableProperty]
    public partial bool EditPeers { get; set; }
    [ObservableProperty]
    public partial string Peers { get; set; } = "";
    [ObservableProperty]
    public partial bool EditListeners { get; set; }
    [ObservableProperty]
    public partial string Listeners { get; set; } = "";

    [ObservableProperty]
    public partial bool EditRpcPortal { get; set; }
    [ObservableProperty]
    public partial string RpcPortal { get; set; } = "";

    [ObservableProperty]
    public partial bool EditProxyNetworks { get; set; }
    [ObservableProperty]
    public partial string ProxyNetworks { get; set; } = "";

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
    public partial bool BindDevice { get; set; } = true;

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
        var configData = Toml.ToModel(File.ReadAllText(configPath));

        InstanceName = configName;
        HostName = configData.Get("hostname", "");

        var networkIdentity = configData.GetTable("network_identity");
        NetworkName = networkIdentity?.Get("network_name", "") ?? "";
        NetworkSecret = networkIdentity?.Get("network_secret", "") ?? "";

        Dhcp = configData.Get("dhcp", false);
        Ipv4 = configData.Get("ipv4", "");

        Peers = configData.GetMultiLinesTextFromTableArray("peer", "uri") ?? "";
        Listeners = configData.GetMultiLinesTextFromArray("listeners") ?? "";
        RpcPortal = configData.Get("rpc_portal", "");
        ProxyNetworks = configData.GetMultiLinesTextFromTableArray("proxy_network", "cidr") ?? "";

        var flags = configData.GetTable("flags");

        LatencyFirst = flags?.Get("latency_first", false) ?? false;
        UseSmoltcp = flags?.Get("use_smoltcp", false) ?? false;
        EnableKcpProxy = flags?.Get("enable_kcp_proxy", false) ?? false;
        DisableKcpInput = flags?.Get("disable_kcp_input", false) ?? false;
        EnableQuicProxy = flags?.Get("enable_quic_proxy", false) ?? false;
        DisableQuicInput = flags?.Get("disable_quic_input", false) ?? false;
        DisableP2p = flags?.Get("disable_p2p", false) ?? false;
        BindDevice = flags?.Get("bind_device", true) ?? true;
        NoTun = flags?.Get("no_tun", false) ?? false;
        EnableExitNode = flags?.Get("enable_exit_node", false) ?? false;
        RelayAllPeerRpc = flags?.Get("relay_all_peer_rpc", false) ?? false;
        MultiThread = flags?.Get("multi_thread", true) ?? true;
        ProxyForwardBySystem = flags?.Get("proxy_forward_by_system", false) ?? false;
        DisableEncryption = !(flags?.Get("enable_encryption", true) ?? true);
        DisableUdpHolePunching = flags?.Get("disable_udp_hole_punching", false) ?? false;
        AcceptDns = flags?.Get("accept_dns", false) ?? false;
        PrivateMode = flags?.Get("private_mode", false) ?? false;
    }

    [RelayCommand]
    public void SaveEditConfigs()
    {
        foreach (var config in Configs.ToArray())
        {
            if (!config.IsSelected)
                continue;

            var configPath = Path.Join(AppSettings.ConfigDirectory, config.Name + ".toml");
            var configData = Toml.ToModel(File.ReadAllText(configPath));

            configData["instance_name"] = config.Name;

            // Add new instance_id if not exist
            if (configData.Get("instance_id", "") == "")
                configData["instance_id"] = Guid.NewGuid().ToString();

            configData.Set("hostname", HostName, "");

            if (NetworkName == "" && NetworkSecret == "")
            {
                configData.Remove("network_identity");
            }
            else
            {
                configData.Set("network_identity", new TomlTable
                {
                    ["network_name"] = NetworkName,
                    ["network_secret"] = NetworkSecret
                });
            }

            if (EditIpAddress)
            {
                configData.Set("dhcp", Dhcp, false);
                configData.Set("ipv4", Ipv4, "");
            }

            if (EditPeers)
            {
                configData.SetMultiLinesTextToTableArray("peer", "uri", Peers);
            }

            if (EditListeners)
            {
                configData.SetMultiLinesTextToArray("listeners", Listeners);
            }

            if (EditRpcPortal)
            {
                configData.Set("rpc_portal", RpcPortal, "");
            }

            if (EditProxyNetworks)
            {
                configData.SetMultiLinesTextToTableArray("proxy_network", "cidr", ProxyNetworks);
            }

            var flags = new TomlTable();
            configData["flags"] = flags;

            flags["dev_name"] = config.Name;

            flags.Set("latency_first", LatencyFirst, false);
            flags.Set("use_smoltcp", UseSmoltcp, false);
            flags.Set("enable_kcp_proxy", EnableKcpProxy, false);
            flags.Set("disable_kcp_input", DisableKcpInput, false);
            flags.Set("enable_quic_proxy", EnableQuicProxy, false);
            flags.Set("disable_quic_input", DisableQuicInput, false);
            flags.Set("disable_p2p", DisableP2p, false);
            flags.Set("bind_device", BindDevice, true);
            flags.Set("no_tun", NoTun, false);
            flags.Set("enable_exit_node", EnableExitNode, false);
            flags.Set("relay_all_peer_rpc", RelayAllPeerRpc, false);
            flags.Set("multi_thread", MultiThread, true);
            flags.Set("proxy_forward_by_system", ProxyForwardBySystem, false);
            flags.Set("enable_encryption", !DisableEncryption, true);
            flags.Set("disable_udp_hole_punching", DisableUdpHolePunching, false);
            flags.Set("accept_dns", AcceptDns, false);
            flags.Set("private_mode", PrivateMode, false);

            File.WriteAllText(configPath, Toml.FromModel(configData));
        }

        CloseEditConfigs();
    }

    [RelayCommand]
    public void CloseEditConfigs()
    {
        IsEditingConfigs = false;
        MainGrid.RowDefinitions[0].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
        MainGrid.RowDefinitions[1].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
    }
}