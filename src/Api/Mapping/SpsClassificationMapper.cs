using Trade.Gateway.Api.Contract;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsClassificationMapper
{
    internal static ProductClassification Map(SPSClassificationType source, MappingContext context) => new()
    {
        SystemId = source.SystemID?.Value,
        SystemName = source.SystemName.ForLanguage(context.LanguageCode),
        ClassCode = source.ClassCode?.Value,
        ClassName = source.ClassName.ForLanguageList(context.LanguageCode)
    };

    internal static List<ProductClassification>? MapList(SPSClassificationType[]? source, MappingContext context) =>
        source?.Select(s => Map(s, context)).ToList().NullIfEmpty();
}
