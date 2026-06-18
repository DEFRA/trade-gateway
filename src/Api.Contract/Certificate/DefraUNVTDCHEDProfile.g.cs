#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record DefraUNVTDCHEDProfile
{
    [JsonPropertyName("$model")]
    [ConstValue("defra/certificate-internal/1")]
    public string Model { get; init; } = "defra/certificate-internal/1";

    [JsonPropertyName("$type")]
    [ConstValue("intra")]
    public string Type { get; init; } = "ched";

    [JsonPropertyName("exchangedDocument")] 
    public required ExchangedDocument ExchangedDocument { get; init; }

    [JsonPropertyName("specifiedConsignment")]
    public required Consignment SpecifiedConsignment { get; init; }
}
