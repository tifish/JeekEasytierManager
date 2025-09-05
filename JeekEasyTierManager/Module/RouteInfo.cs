namespace JeekEasyTierManager;

using System.Text.Json;
using System.Text.Json.Serialization;

public class RouteInfo
{
    [JsonPropertyName("ipv4")]
    public string Ipv4 { get; set; } = "";

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = "";

    [JsonPropertyName("proxy_cidrs")]
    public string ProxyCidrs { get; set; } = "";

    [JsonPropertyName("next_hop_ipv4")]
    public string NextHopIpv4 { get; set; } = "";

    [JsonPropertyName("next_hop_hostname")]
    public string NextHopHostname { get; set; } = "";

    [JsonPropertyName("next_hop_lat")]
    public double NextHopLat { get; set; }

    [JsonPropertyName("path_len")]
    public int PathLen { get; set; }

    [JsonPropertyName("path_latency")]
    public int PathLatency { get; set; }

    [JsonPropertyName("next_hop_ipv4_lat_first")]
    public string NextHopIpv4LatFirst { get; set; } = "";

    [JsonPropertyName("next_hop_hostname_lat_first")]
    public string NextHopHostnameLatFirst { get; set; } = "";

    [JsonPropertyName("path_len_lat_first")]
    public int PathLenLatFirst { get; set; }

    [JsonPropertyName("path_latency_lat_first")]
    public int PathLatencyLatFirst { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}

