#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record Authentication
{
    [JsonPropertyName("typeCode")]
    [Description("unece:typeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("actualDateTime")]
    public string? ActualDateTime { get; init; }

    [JsonPropertyName("governmentActionTypeCode")]
    [Description("unece:governmentActionTypeCode is @vocab in unece-context-D23B.jsonld; use a string code or vocabulary token.")]
    public string? GovernmentActionTypeCode { get; init; }

    [JsonPropertyName("providerParty")]
    public TradeParty? ProviderParty { get; init; }

    [JsonPropertyName("includedClause")]
    public List<Clause>? IncludedClause { get; init; }
}
