#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LogisticsTransportMovement
{
    [JsonPropertyName("identifier")]
    [Description("Transport identifier as a bare string or integer (legacy INTRA shape where it was sometimes numeric). Matches the BSP D23B canonical shape where idType metadata is disabled. The sibling urlId names the register the identifier is drawn from (e.g. vessel_name, road_vehicle_registration, airplane_flight_number).")]
    public string? Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL identifier for the codelist / register this movement's identifier is drawn from.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("modeCode")]
    [Description("Mode-of-transport code per UN/EDIFACT Recommendation 19. Accepts integer (current INTRA/CHED shape) or string (hybrid shape where GBN-AG and TRACES-textual consumers prefer it as a code value).")]
    public string? ModeCode { get; init; }

    [JsonPropertyName("usedLogisticsTransportMeans")]
    [Description("The conveyance used on this leg. `name` carries the means' identifying name (vessel name, vehicle registration, flight callsign). Distinct from `identifier` on the parent movement, which carries the movement's reference (journey number, waybill, booking). Both slots can be populated and may carry different values: identifier identifies the movement, usedLogisticsTransportMeans.name identifies the vehicle / vessel / aircraft.")]
    public LogisticsTransportMovementUsedLogisticsTransportMeans? UsedLogisticsTransportMeans { get; init; }

    [JsonPropertyName("transportContractRelatedReferencedDocument")]
    public List<ReferencedDocument>? TransportContractRelatedReferencedDocument { get; init; }

    [JsonPropertyName("arrivalEvent")]
    public List<TransportEvent>? ArrivalEvent { get; init; }

    [JsonPropertyName("departureEvent")]
    public List<TransportEvent>? DepartureEvent { get; init; }
}

public partial record LogisticsTransportMovementUsedLogisticsTransportMeans
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
