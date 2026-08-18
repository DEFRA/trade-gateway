#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record UneceMeasure
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("unitCode")]
    [Description("Measurement unit code token.")]
    public string? UnitCode { get; init; }

    [JsonPropertyName("unitCodeListVersionId")]
    public string? UnitCodeListVersionId { get; init; }
}
