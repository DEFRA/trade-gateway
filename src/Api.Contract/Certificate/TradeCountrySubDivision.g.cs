#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TradeCountrySubDivision
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the codelist the `identifier` is drawn from, when a sub-division carries one.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("functionTypeCode")]
    public required TradeCountrySubDivisionFunctionTypeCode FunctionTypeCode { get; init; }

    [JsonPropertyName("activityAuthorizedParty")]
    [Description("Parties attached to this sub-division, distinguished by `partyRoleCode`. On an authority sub-division ('44', '42', '41'): 'RA' central competent authority, 'VG' local competent authority, 'CM' customs / border control post. On a non-authority one the party is the operator — 'EX' exporter on a government approved establishment ('272'), 'DP' on a designated goods disposal location ('283'). Absent on a region ('106'), which carries only a name.")]
    public List<TradeParty>? ActivityAuthorizedParty { get; init; }
}

public partial record TradeCountrySubDivisionFunctionTypeCode
{
    [JsonPropertyName("content")]
    [Description("UNCL3227 location function code. Deliberately an open string, not an enum: TRACES emits this code from the wider UNCL3227 list (see https://service.unece.org/trade/untdid/d95a/uncl/uncl3227.htm), and an enumerated list would be a snapshot that goes stale on the next certificate type or UNCEFACT release. The code tells you how to read the rest of the sub-division: '106' is a region of origin; the others ('44' place of authentication, '42'/'41' consignment exit/entry customs office, '272' government approved establishment, '283' goods disposal location) carry the parties on `activityAuthorizedParty`.")]
    public required string Content { get; init; }
}
