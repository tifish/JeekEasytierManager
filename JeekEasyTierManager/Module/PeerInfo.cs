namespace JeekEasyTierManager;

using System.Text.Json.Serialization;

/// <summary>
/// Represents peer information for serialization/deserialization.
/// </summary>
public class PeerInfo
{
    [JsonPropertyName("cidr")]
    public string Cidr { get; set; } = "";

    [JsonPropertyName("ipv4")]
    public string Ipv4 { get; set; } = "";

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = "";

    [JsonPropertyName("cost")]
    public string Cost { get; set; } = "";

    [JsonPropertyName("lat_ms")]
    public double LatMs { get; set; }

    [JsonPropertyName("loss_rate")]
    public double LossRate { get; set; }

    [JsonPropertyName("rx_bytes")]
    public string RxBytes { get; set; } = "";

    [JsonPropertyName("tx_bytes")]
    public string TxBytes { get; set; } = "";

    [JsonPropertyName("tunnel_proto")]
    public string TunnelProto { get; set; } = "";

    [JsonPropertyName("nat_type")]
    public string NatType { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}
