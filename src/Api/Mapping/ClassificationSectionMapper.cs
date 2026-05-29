using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class ClassificationSectionMapper
{
    internal static ClassificationSection Map(ClassificationSectionType source) =>
        new()
        {
            ClassCode = source.code,
            Chapter = source.ClassificationSectionChapter?.Value,
            Lms = source.lms,
            Description = source.Description.Value,
            Active = source.active,
            Scopes = source.MetaCountryGroupScope
                ?.Select(scope => scope.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToList() ?? [],
        };

    internal static ClassificationSection Map(ClassificationSectionReference source) =>
        new()
        {
            ClassCode = source.code,
            Chapter = source.chapter,
            Lms = source.lms,
            Description = source.Description.Value,
            Scopes = source.Scope
                ?.Select(scope => scope.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToList() ?? [],
        };


    internal static DefraUNVTDProfileClassificationSectionListResponse Map(
        ClassificationSectionType[] source
    ) =>
        new()
        {
            Sections = source.Select(Map)
                .ToList()
                .NullIfEmpty(),
            RetrievedAt = DateTimeOffset.UtcNow,
        };

}
