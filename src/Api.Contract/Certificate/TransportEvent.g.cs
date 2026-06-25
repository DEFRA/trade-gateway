#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TransportEvent
{
    [JsonPropertyName("scheduledOccurrenceDateTime")]
    public DateTimeOffset? ScheduledOccurrenceDateTime { get; init; }

    [JsonPropertyName("actualOccurrenceDateTime")]
    public DateTimeOffset? ActualOccurrenceDateTime { get; init; }

    [JsonPropertyName("occurrenceLogisticsLocation")]
    public LogisticsLocation? OccurrenceLogisticsLocation { get; init; }
}
