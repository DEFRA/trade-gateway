using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.ReferenceData;

namespace Api.Mapping;

internal static class ClassificationSectionNodeAttributeMapper
{
    internal static ClassificationSectionGroup Map(ClassificationSectionNodeAttribute source) =>
        new()
        {
            Id = source.id,
            Description = source.Description.Value,
            Sections = source.ClassificationSection.Select(ClassificationSectionMapper.Map).ToList(),
        };
}
