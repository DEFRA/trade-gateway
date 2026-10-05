using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsExaminationEventMapper
{
    internal static List<ExaminationEvent>? MapList(SPSEventType? source, MappingContext context)
    {
        if (source?.OccurrenceSPSLocation is not { } location)
            return null;

        var mapped = SpsLocationMapper.Map(location, context);
        if (mapped is { Identifier: null, UrlId: null, Name: null })
            return null;

        return [new ExaminationEvent { OccurrenceLogisticsLocation = mapped }];
    }
}
