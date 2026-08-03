namespace Trade.Gateway.Api.Contract.Events;

public class EventEnvelopeMetadata
{
    public required string CorrelationId { get; set; }
    public required string SchemaVersion { get; set; }
    public required Uri SchemaUri { get; set; }
}
