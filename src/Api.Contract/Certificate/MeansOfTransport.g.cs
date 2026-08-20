#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record MeansOfTransport
{
    [JsonPropertyName("specifiedLogisticsTransportMovement")]
    [Description("The movement for this leg (TRACES SPSTransportMovement). identifier is the conveyance identifier, with urlId naming the register it is drawn from (e.g. road_vehicle_registration_after_bcp); modeCode carries the mode per UN/ECE Recommendation 19. Aliased to unece:specifiedTransportMovement.")]
    public LogisticsTransportMovement? SpecifiedLogisticsTransportMovement { get; init; }

    [JsonPropertyName("internationalTransportDocument")]
    [Description("Reference of the international transport document covering this leg (TRACES InternationalTrasportDocument — the TRACES element name carries a spelling error that is corrected here).")]
    public string? InternationalTransportDocument { get; init; }
}
