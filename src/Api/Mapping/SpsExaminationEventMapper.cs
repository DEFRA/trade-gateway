using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsExaminationEventMapper
{
    internal static List<ExaminationEvent>? MapList(SPSEventType? source, MappingContext context) =>
        SpsLocationMapper.Map(source?.OccurrenceSPSLocation, context) is { } location
            ? [new ExaminationEvent { OccurrenceLogisticsLocation = location }]
            : null;
}
