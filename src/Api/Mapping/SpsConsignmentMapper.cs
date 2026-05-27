using Trade.Gateway.Api.Contract;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsConsignmentMapper
{
    internal static Consignment Map(SPSConsignmentType source, MappingContext context) => new()
    {
        AvailabilityDueDateTime = SpsDateTimeMapper.Map(source.AvailabilityDueDateTime),
        ExportExitDateTime = SpsDateTimeMapper.Map(source.ExportExitDateTime),
        ConsignorParty = SpsPartyMapper.Map(source.ConsignorSPSParty),
        ConsigneeParty = SpsPartyMapper.Map(source.ConsigneeSPSParty),
        DespatchParty = SpsPartyMapper.Map(source.DespatchSPSParty),
        CustomsTransitAgentParty = SpsPartyMapper.Map(source.CustomsTransitAgentSPSParty),
        ExportCountry = SpsCountryMapper.Map(source.ExportSPSCountry, context),
        ImportCountry = SpsCountryMapper.Map(source.ImportSPSCountry, context),
        ReExportCountry = SpsCountryMapper.MapList(source.ReExportSPSCountry, context),
        TransitCountry = SpsCountryMapper.MapList(source.TransitSPSCountry, context),
        UnloadingBaseportLocation = null,
        IncludedConsignmentItem = SpsConsignmentItemMapper.MapList(source.IncludedSPSConsignmentItem, context)
    };
}
