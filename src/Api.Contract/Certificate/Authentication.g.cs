#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record Authentication
{
    [JsonPropertyName("typeCode")]
    [Description("unece:typeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("actualDateTime")]
    public DateTimeOffset? ActualDateTime { get; init; }

    [JsonPropertyName("governmentActionTypeCode")]
    [Description("unece:governmentActionTypeCode is @vocab in unece-context-D23B.jsonld; use a string code or vocabulary token.")]
    public string? GovernmentActionTypeCode { get; init; }

    [JsonPropertyName("provider")]
    [Description("The party authenticating this signatory entry (the issuing veterinarian, the certifying authority, the inspecting body). Maps to UN/CEFACT documentAuthentication.provider. Renamed from providerParty per TIG §4.4 naming alignment.")]
    public TradeParty? Provider { get; init; }

    [JsonPropertyName("includedClause")]
    public List<Clause>? IncludedClause { get; init; }
}
