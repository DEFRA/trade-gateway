using Api.Constants;
using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class MetadataMapper
{
    internal static DefraUNVTDProfileMetadataListResponse Map(MetadataCodeType[] source, string metadataType)
    {
        return new DefraUNVTDProfileMetadataListResponse
        {
            MetadataType = metadataType,
            Items = source.Select(MetadataCodeMapper.Map).ToList(),
            RetrievedAt = DateTime.UtcNow,
            Source = ReferenceDataSource.Traces
        };
    }
}
