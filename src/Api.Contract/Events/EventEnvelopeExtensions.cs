using Trade.Gateway.Api.Contract.Certificate;

namespace Trade.Gateway.Api.Contract.Events;

public static class EventEnvelopeExtensions
{
    public static EventEnvelope<DefraUNVTDCHEDProfile> ToEventEnvelope(
        this DefraUNVTDCHEDProfile data,
        string correlationId
    )
    {
        return new EventEnvelope<DefraUNVTDCHEDProfile>()
        {
            EventId = Guid.CreateVersion7(),
            AggregateId =
                $"Imports.{Constants.AggregateTypes.TracesChed}.{data.ExchangedDocument.GetSubType()}.{data.ExchangedDocument.Identifier}",
            AggregateType = Constants.AggregateTypes.TracesChed,
            SubType = data.ExchangedDocument.GetSubType() ?? string.Empty,
            EventType = data.GetType().FullName!,
            Timestamp = DateTime.UtcNow,
            Data = data,
            Metadata = new EventEnvelopeMetadata
            {
                CorrelationId = correlationId,
                SchemaVersion = Constants.Schemas.TracesChedV1.Version,
                SchemaUri = Constants.Schemas.TracesChedV1.Uri,
            },
        };
    }

    public static EventEnvelope<DefraUNVTDINTRAProfile> ToEventEnvelope(
        this DefraUNVTDINTRAProfile data,
        string correlationId
    )
    {
        return new EventEnvelope<DefraUNVTDINTRAProfile>()
        {
            EventId = Guid.CreateVersion7(),
            AggregateId =
                $"Imports.{Constants.AggregateTypes.TracesIntra}.{data.ExchangedDocument.GetSubType()}.{data.ExchangedDocument.Identifier}",
            AggregateType = Constants.AggregateTypes.TracesIntra,
            SubType = data.ExchangedDocument.GetSubType() ?? string.Empty,
            EventType = data.GetType().FullName!,
            Timestamp = DateTime.UtcNow,
            Data = data,
            Metadata = new EventEnvelopeMetadata
            {
                CorrelationId = correlationId,
                SchemaVersion = Constants.Schemas.TracesIntraV1.Version,
                SchemaUri = Constants.Schemas.TracesIntraV1.Uri,
            },
        };
    }

    public static string? GetSubType(this ExchangedDocument obj)
    {
        return obj.Identifier.Split('.').FirstOrDefault();
    }
}
