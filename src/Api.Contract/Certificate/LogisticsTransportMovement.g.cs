#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LogisticsTransportMovement
{
    [JsonPropertyName("identifier")]
    [Description("The declared transport identification for this movement, as a bare string or integer (legacy INTRA shape where it was sometimes numeric). Matches the BSP D23B canonical shape where idType metadata is disabled. The value's register is named by the sibling urlId and varies by mode - road_vehicle_registration for road, vessel_name for maritime, airplane_flight_number for air - mirroring the schemeID that TRACES carries on the movement's ID. This is the single home for the means-of-transport identification; there is no separate conveyance-name slot.")]
    public string? Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL identifier for the codelist / register this movement's identifier is drawn from.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("modeCode")]
    [Description("Mode-of-transport code, held as an integer. From the UN/CEFACT TransportModeCodeList (UN/EDIFACT Recommendation 19): 1 Maritime, 2 Rail, 3 Road, 4 Air.")]
    public int? ModeCode { get; init; }

    [JsonPropertyName("transportContractRelatedReferencedDocument")]
    public List<ReferencedDocument>? TransportContractRelatedReferencedDocument { get; init; }

    [JsonPropertyName("arrivalEvent")]
    public List<TransportEvent>? ArrivalEvent { get; init; }

    [JsonPropertyName("departureEvent")]
    public List<TransportEvent>? DepartureEvent { get; init; }
}
