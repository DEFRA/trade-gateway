#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record RedispatchDetails
{
    [JsonPropertyName("redispatchDateTime")]
    [Description("When the consignment was redispatched.")]
    public DateTimeOffset? RedispatchDateTime { get; init; }

    [JsonPropertyName("exitAuthorityParty")]
    [Description("The authority at the point of exit that handled the redispatch (TRACES ExitAuthoritySPSParty) — typically a border control post. identifier is drawn from authority_activity_id.")]
    public TradeParty? ExitAuthorityParty { get; init; }

    [JsonPropertyName("destinationCountry")]
    [Description("The country the consignment was redispatched to (TRACES CountryOfDestination, unece:destinationCountry).")]
    public TradeCountry? DestinationCountry { get; init; }

    [JsonPropertyName("meansOfTransport")]
    [Description("The transport used for the redispatch, one entry per leg (TRACES MeansOfTransport).")]
    public List<MeansOfTransport>? MeansOfTransport { get; init; }

    [JsonPropertyName("placeOfDestinationParty")]
    [Description("The operator at the redispatch destination (TRACES PlaceOfDestinationSPSParty). identifier is drawn from operator_activity_id.")]
    public TradeParty? PlaceOfDestinationParty { get; init; }
}
