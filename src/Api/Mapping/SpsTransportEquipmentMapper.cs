using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsTransportEquipmentMapper
{
    internal static LogisticsTransportEquipment Map(SPSTransportEquipmentType source) =>
        new()
        {
            Identifier = source.ID?.Value,
            UrlId = source.ID?.schemeID.ToCodelistUri(),
            AffixedLogisticsSeal = MapSeals(source.AffixedSPSSeal),
        };

    internal static List<LogisticsTransportEquipment>? MapList(SPSTransportEquipmentType[]? source) =>
        source?.Where(s => s.ID is not null || s.AffixedSPSSeal is { Length: > 0 }).Select(Map).ToList().NullIfEmpty();

    static List<LogisticsSeal>? MapSeals(SPSSealType[]? source) =>
        source
            ?.Select(s => new LogisticsSeal { Identifier = s.ID?.Value, UrlId = s.ID?.schemeID.ToCodelistUri() })
            .ToList()
            .NullIfEmpty();
}
