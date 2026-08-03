namespace Trade.Gateway.Api.Contract.Events;

public class EventEnvelope<TEvent>
    where TEvent : class
{
    public required Guid EventId { get; set; }
    public required string AggregateType { get; set; }
    public required string SubType { get; set; }
    public required string AggregateId { get; set; }
    public int? AggregateVersion { get; set; }
    public required string EventType { get; set; }
    public required DateTime Timestamp { get; set; }
    public required TEvent Data { get; set; }
    public required EventEnvelopeMetadata Metadata { get; set; }
}
