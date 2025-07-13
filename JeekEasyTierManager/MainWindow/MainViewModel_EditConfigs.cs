using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nett;

namespace JeekEasyTierManager;

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
    public partial bool EditFileLogger { get; set; }
    [ObservableProperty]
    public partial bool EnableFileLogger { get; set; }
    [ObservableProperty]
    public partial string FileLoggerName { get; set; } = "";
    [ObservableProperty]
    public partial string FileLoggerDir { get; set; } = "";
    [ObservableProperty]
    public partial string FileLoggerLevel { get; set; } = "error";
    [ObservableProperty]
    public partial ObservableCollection<string> FileLoggerLevels { get; set; } = new() { "error", "warn", "info", "debug" };

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
        var configData = Toml.ReadFile(configPath);

        InstanceName = configName;
        HostName = configData.Get("hostname", "");

        var networkIdentity = configData.GetTable("network_identity");
        NetworkName = networkIdentity.Get("network_name", "");
        NetworkSecret = networkIdentity.Get("network_secret", "");

        Dhcp = configData.Get("dhcp", false);
        Ipv4 = configData.Get("ipv4", "");

        Peers = configData.GetMultiLinesTextFromTableArray("peer", "uri");
        Listeners = configData.GetMultiLinesTextFromArray("listeners");
        RpcPortal = configData.Get("rpc_portal", "");
        ProxyNetworks = configData.GetMultiLinesTextFromTableArray("proxy_network", "cidr");

        var fileLogger = configData.GetTable("file_logger");
        EnableFileLogger = fileLogger != null;
        FileLoggerName = configName;
        FileLoggerDir = fileLogger.Get("dir", "");
        FileLoggerLevel = fileLogger.Get("level", "error");

        var flags = configData.GetTable("flags");

        LatencyFirst = flags.Get("latency_first", false);
        UseSmoltcp = flags.Get("use_smoltcp", false);
        EnableKcpProxy = flags.Get("enable_kcp_proxy", false);
        DisableKcpInput = flags.Get("disable_kcp_input", false);
        EnableQuicProxy = flags.Get("enable_quic_proxy", false);
        DisableQuicInput = flags.Get("disable_quic_input", false);
        DisableP2p = flags.Get("disable_p2p", false);
        BindDevice = flags.Get("bind_device", true);
        NoTun = flags.Get("no_tun", false);
        EnableExitNode = flags.Get("enable_exit_node", false);
        RelayAllPeerRpc = flags.Get("relay_all_peer_rpc", false);
        MultiThread = flags.Get("multi_thread", true);
        ProxyForwardBySystem = flags.Get("proxy_forward_by_system", false);
        DisableEncryption = !flags.Get("enable_encryption", true);
        DisableUdpHolePunching = flags.Get("disable_udp_hole_punching", false);
        AcceptDns = flags.Get("accept_dns", false);
        PrivateMode = flags.Get("private_mode", false);
    }

    [RelayCommand]
    public void SaveEditConfigs()
    {
        if (InstanceName == MultipleConfigInstanceName)
        {
            foreach (var config in Configs.ToArray())
            {
                if (!config.IsSelected)
                    continue;

                SaveConfig(config.Name);
            }
        }
        else
        {
            SaveConfig(InstanceName);
        }

        CloseEditConfigs();
    }

    private void SaveConfig(string configName)
    {
        var configPath = Path.Join(AppSettings.ConfigDirectory, configName + ".toml");
        var configData = Toml.ReadFile(configPath);

        configData.Set("instance_name", configName);

        // Add new instance_id if not exist
        if (configData.Get("instance_id", "") == "")
            configData.Set("instance_id", Guid.NewGuid().ToString());

        configData.Set("hostname", HostName, "");

        if (NetworkName == "" && NetworkSecret == "")
        {
            configData.Remove("network_identity");
        }
        else
        {
            configData.Set("network_identity", new Dictionary<string, object>()
                {
                    {"network_name", NetworkName},
                    {"network_secret", NetworkSecret}
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

        if (EditFileLogger)
        {
            if (EnableFileLogger)
            {
                configData.Set("file_logger", new Dictionary<string, object>()
                    {
                        {"file", configName},
                        {"dir", FileLoggerDir},
                        {"level", FileLoggerLevel}
                    });
            }
            else
            {
                configData.Remove("file_logger");
            }
        }

        var flags = configData.CreateEmptyAttachedTable();
        configData["flags"] = flags;

        flags.Set("dev_name", configName);

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

        Toml.WriteFile(configData, configPath);
    }

    [RelayCommand]
    public void CloseEditConfigs()
    {
        IsEditingConfigs = false;
        MainGrid.RowDefinitions[0].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
        MainGrid.RowDefinitions[1].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
    }

    [RelayCommand]
    public void SetDefaultRpcPortal()
    {
        RpcPortal = "127.0.0.1:15888";
    }

    [RelayCommand]
    public void SetCurrentProxyNetworks()
    {
        // 通过创建一个 socket 访问一个全球可达的地址，获取到当前主网卡的子网
        try
        {
            // 连接到一个国内常用的公网地址（如 114.114.114.114:53），以获取本地出口IP
            using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
            {
                // 114.114.114.114 是中国大陆常用的公共DNS服务器
                socket.Connect("114.114.114.114", 53);
                var localEndPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                if (localEndPoint != null)
                {
                    var localIp = localEndPoint.Address;
                    // 获取所有本地网卡信息
                    var allNics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var nic in allNics)
                    {
                        if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                            continue;
                        var ipProps = nic.GetIPProperties();
                        foreach (var unicast in ipProps.UnicastAddresses)
                        {
                            if (unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                unicast.Address.Equals(localIp))
                            {
                                // 计算子网
                                var ipBytes = unicast.Address.GetAddressBytes();
                                var maskBytes = unicast.IPv4Mask.GetAddressBytes();
                                var networkBytes = new byte[4];
                                for (int i = 0; i < 4; i++)
                                {
                                    networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                                }
                                var networkAddress = new System.Net.IPAddress(networkBytes);
                                // 计算掩码长度
                                int maskLength = 0;
                                foreach (var b in maskBytes)
                                {
                                    for (int i = 7; i >= 0; i--)
                                    {
                                        if ((b & (1 << i)) != 0)
                                            maskLength++;
                                    }
                                }
                                ProxyNetworks = $"{networkAddress}/{maskLength}";
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Messages += $"\n获取子网失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SetDefaultPeers()
    {
        Peers = "tcp://public.easytier.top:11010";
    }

    [RelayCommand]
    public void SetRandomNetworkSecret()
    {
        NetworkSecret = Guid.NewGuid().ToString()[..14].Replace("-", "");
    }
}