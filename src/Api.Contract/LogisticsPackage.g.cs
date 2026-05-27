#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record LogisticsPackage
{
    [JsonPropertyName("levelCode")]
    public int? LevelCode { get; init; }

    [JsonPropertyName("typeCode")]
    [Description("unece:typeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("itemQuantity")]
    public int? ItemQuantity { get; init; }
}
