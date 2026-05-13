using System.Text.Json.Serialization;

namespace Api.Models;

[MediaType("application/vnd.defra.unvtd.profile.intra.v1+json")]
public class IntraCertificate
{
    [JsonPropertyName("Ref")]
    public required string Ref { get; set; }

    [JsonPropertyName("Consignment")]
    public Consignment? Consignment { get; set; }
}

[MediaType("application/vnd.defra.unvtd.profile.intra.v2+json")]
public class IntraCertificateV2
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("consignment")]
    public Consignment? Consignment { get; set; }
}

public class Consignment
{
    public string? Package { get; set; }
}