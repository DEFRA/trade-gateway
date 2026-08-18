using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsTradeLineItemMapper
{
    private const string LatinLanguageId = "la";

    internal static TradeLineItem Map(SPSTradeLineItemType source, MappingContext context) =>
        new()
        {
            SequenceNumeric = source.SequenceNumeric is { } sn ? (int)sn.Value : null,
            Description = source.Description.ForLanguageList(context.LanguageCode),
            ScientificName = source.ScientificName.ForLanguage(LatinLanguageId),
            NetWeight = SpsMeasureMapper.Map(source.NetWeightMeasure),
            GrossWeight = SpsMeasureMapper.Map(source.GrossWeightMeasure),
            ApplicableClassification = SpsClassificationMapper.MapList(source.ApplicableSPSClassification, context),
            PhysicalReferencedLogisticsPackage = SpsPackageMapper.MapList(source.PhysicalSPSPackage),
        };

    internal static List<TradeLineItem>? MapList(SPSTradeLineItemType[]? source, MappingContext context) =>
        source?.Select(s => Map(s, context)).ToList().NullIfEmpty();
}
