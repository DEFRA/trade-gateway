using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class ClassificationSectionGroupMapper
{
    internal static ClassificationSectionGroup Map(ClassificationSectionNodeAttribute source) =>
        new()
        {
            Id = source.id,
            Description = source.Description.Value,
            Sections = source.ClassificationSection.Select(ClassificationSectionMapper.Map).ToList()
        };
}
