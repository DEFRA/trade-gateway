using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class MetadataCodeMapper
{
    internal static MetadataCode Map(MetadataCodeType source) =>
        new()
        {
            Value = source.Value,
            MappedValue = source.mappedValue,
            Active = source.active,
        };
}
