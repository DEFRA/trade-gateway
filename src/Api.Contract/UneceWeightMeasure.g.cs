#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record UneceWeightMeasure
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("unitCode")]
    [Description("Weight unit code (subset of UN/CEFACT Rec 20/21 used by this profile).")]
    public string? UnitCode { get; init; }

    [JsonPropertyName("unitCodeListVersionId")]
    public string? UnitCodeListVersionId { get; init; }
}
