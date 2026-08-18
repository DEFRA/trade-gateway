#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record DefraUNVTDDOCOMProfile
{
    [JsonPropertyName("$model")]
    [ConstValue("defra/certificate-internal/1")]
    public string Model { get; init; } = "defra/certificate-internal/1";

    [JsonPropertyName("$type")]
    [ConstValue("docom")]
    public string Type { get; init; } = "docom";

    [JsonPropertyName("exchangedDocument")]
    public required ExchangedDocument ExchangedDocument { get; init; }

    [JsonPropertyName("specifiedConsignment")]
    public required Consignment SpecifiedConsignment { get; init; }

    [JsonPropertyName("laboratoryObservationResult")]
    [Description("UN vocabulary-aligned laboratory observations/results collection.")]
    public List<LaboratoryObservationResult>? LaboratoryObservationResult { get; init; }
}
