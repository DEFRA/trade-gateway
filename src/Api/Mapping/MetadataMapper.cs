using Api.Constants;
using Trade.Gateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class MetadataMapper
{
    internal static DefraUNVTDProfileMetadataListResponse Map(MetadataCodeType[] source, string metadataType)
    {
        return new DefraUNVTDProfileMetadataListResponse
        {
            Source = ReferenceDataSource.Traces,
            MetadataType = metadataType,
            Items = source.Select(MetadataCodeMapper.Map).ToList(),
            RetrievedAt = DateTimeOffset.UtcNow
        };
    }
}
