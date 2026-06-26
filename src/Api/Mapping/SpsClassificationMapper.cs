using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsClassificationMapper
{
#pragma warning disable S1075
    const string ClassCodeUri = "https://traces-codelists.ec.europa.eu/class_code_system/{0}";
#pragma warning restore S1075
    
    internal static ApplicableClassification Map(SPSClassificationType source, MappingContext context) =>
        new()
        {
            SystemId = source.SystemID?.Value,
            SystemName = source.SystemName.ForLanguage(context.LanguageCode),
            ClassCode = source.ClassCode?.Value.ToCodedValue(string.Format(ClassCodeUri, source.SystemID?.Value ?? "")),
            ClassName = source.ClassName.ForLanguageList(context.LanguageCode)
        };

    internal static List<ApplicableClassification>? MapList(SPSClassificationType[]? source, MappingContext context) =>
        source?.Select(s => Map(s, context)).ToList().NullIfEmpty();
}
