using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsConsignmentItemMapper
{
    internal static ConsignmentItem Map(SPSConsignmentItemType source, MappingContext context) =>
        new()
        {
            NatureIdCargo = SpsCargoMapper.MapList(source.NatureIdentificationSPSCargo),
            IncludedTradeLineItem = SpsTradeLineItemMapper.MapList(source.IncludedSPSTradeLineItem, context),
        };

    internal static List<ConsignmentItem>? MapList(SPSConsignmentItemType[]? source, MappingContext context) =>
        source?.Select(s => Map(s, context)).ToList().NullIfEmpty();
}
