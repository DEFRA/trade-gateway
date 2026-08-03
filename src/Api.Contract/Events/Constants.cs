namespace Trade.Gateway.Api.Contract.Events;

public static class Constants
{
    public static class AggregateTypes
    {
        public const string TracesChed = "TracesChed";
        public const string TracesIntra = "TracesIntra";
    }

    public static class Schemas
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        internal const string TracesChedV1Uri =
            "https://github.com/DEFRA/trade-imports-schemas/blob/main/schemas/profiles/imports/international/defra-unvtd-profile-ched-v1.schema.json";

        internal const string TracesIntraV1Uri =
            "https://github.com/DEFRA/trade-imports-schemas/blob/main/schemas/profiles/imports/eu/defra-unvtd-profile-intra-v1.schema.json";
#pragma warning restore S1075 // URIs should not be hardcoded

        public static SchemaMetadata TracesChedV1 => new() { Uri = new Uri(TracesChedV1Uri), Version = "1.0.0" };
        public static SchemaMetadata TracesIntraV1 => new() { Uri = new Uri(TracesIntraV1Uri), Version = "1.0.0" };
    }
}
