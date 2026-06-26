using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsConsignmentMapper
{
    internal static Consignment Map(SPSConsignmentType source, MappingContext context) =>
        new()
        {
            AvailabilityDueDateTime = SpsDateTimeMapper.Map(source.AvailabilityDueDateTime),
            ExportExitDateTime = SpsDateTimeMapper.Map(source.ExportExitDateTime),
            ConsignorParty = SpsPartyMapper.Map(source.ConsignorSPSParty),
            ConsigneeParty = SpsPartyMapper.Map(source.ConsigneeSPSParty),
            DeliveryParty = SpsPartyMapper.Map(source.DeliverySPSParty),
            DespatchParty = SpsPartyMapper.Map(source.DespatchSPSParty),
            CustomsTransitAgentParty = SpsPartyMapper.Map(source.CustomsTransitAgentSPSParty),
            ExportCountry = SpsCountryMapper.Map(source.ExportSPSCountry, context),
            ImportCountry = SpsCountryMapper.Map(source.ImportSPSCountry, context),
            ReExportCountry = SpsCountryMapper.MapList(source.ReExportSPSCountry, context),
            TransitCountry = SpsCountryMapper.MapList(source.TransitSPSCountry, context),
            UnloadingBaseportLocation = SpsLocationMapper.Map(source.UnloadingBaseportSPSLocation, context),
            IncludedConsignmentItem = SpsConsignmentItemMapper.MapList(source.IncludedSPSConsignmentItem, context),
            MainCarriageLogisticsTransportMovement = SpsTransportMovementMapper.MapList(source.MainCarriageSPSTransportMovement)
        };
}
