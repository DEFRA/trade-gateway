#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record Clause
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("content")]
    [Description("unece:content is xsd:string in unece-context-D23B.jsonld.")]
    public string? Content { get; init; }
}
