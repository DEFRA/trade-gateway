#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record TradeParty
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("name")]
    [Description("unece:name is xsd:string in unece-context-D23B.jsonld.")]
    public string? Name { get; init; }

    [JsonPropertyName("partyRoleCode")]
    [Description("unece:partyRoleCode is @vocab in unece-context-D23B.jsonld.")]
    public string? PartyRoleCode { get; init; }

    [JsonPropertyName("partyTypeCode")]
    public JsonElement? PartyTypeCode { get; init; }

    [JsonPropertyName("postalAddress")]
    public TradeAddress? PostalAddress { get; init; }

    [JsonPropertyName("definedContact")]
    public List<TradePartyDefinedContactItem>? DefinedContact { get; init; }
}

public partial record TradePartyDefinedContactItem
{
    [JsonPropertyName("personName")]
    public string? PersonName { get; init; }
}
