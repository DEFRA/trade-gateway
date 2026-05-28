#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record CertificatePayload
{
    [JsonPropertyName("$model")]
    public required string Model { get; init; }

    [JsonPropertyName("$type")]
    public required string Type { get; init; }

    [JsonPropertyName("exchangedDocument")]
    public required ExchangedDocument ExchangedDocument { get; init; }

    [JsonPropertyName("specifiedConsignment")]
    public required Consignment SpecifiedConsignment { get; init; }

    [JsonPropertyName("laboratoryObservationResult")]
    [Description("UN vocabulary-aligned laboratory observations/results collection.")]
    public List<LaboratoryObservationResult>? LaboratoryObservationResult { get; init; }
}
