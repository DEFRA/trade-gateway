using System.Diagnostics;
using Api.Constants;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.ReferenceData;

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
            Scopes =
                source
                    .MetaCountryGroupScope?.Select(scope => scope.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToList()
                ?? [],
            OperatorActivities =
                source
                    .OperatorActivityType?.Select(activity => activity.Value) // note - when within a ClassificationSectionType the value is used to hold the activity type
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToList()
                ?? [],
        };

    internal static ClassificationSection Map(ClassificationSectionReference source) =>
        new()
        {
            ClassCode = source.code,
            Chapter = source.chapter,
            Lms = source.lms,
            Description = source.Description.Value,
            Scopes =
                source
                    .Scope?.Select(scope => scope.id)
                    .Where(scopeId => !string.IsNullOrWhiteSpace(scopeId))
                    .Select(scopeId => scopeId!)
                    .ToList()
                ?? [],
            OperatorActivities =
                source
                    .OperatorActivityType?.Select(activity => activity.type) // note - when within a ClassificationSectionReference the activity.type is used to hold the activity type
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToList()
                ?? [],
        };

    internal static DefraUNVTDProfileClassificationSectionListResponse Map(ClassificationSectionType[] source) =>
        new()
        {
            Source = ReferenceDataSource.Traces,
            Service = ReferenceDataService.ReferenceDataServiceV1,
            Sections = source.Select(Map).ToList().NullIfEmpty(),
            RetrievedAt = DateTimeOffset.UtcNow,
        };
}
