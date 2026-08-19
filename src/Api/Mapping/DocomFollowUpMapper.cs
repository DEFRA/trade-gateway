using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class DocomFollowUpMapper
{
    internal static List<FollowUpRecord>? MapList(DocomFollowUpType[]? source, MappingContext context) =>
        source?.Select(s => Map(s, context)).ToList().NullIfEmpty();

    private static FollowUpRecord Map(DocomFollowUpType source, MappingContext context) =>
        new()
        {
            CreationDateTime = SpsDateTimeMapper.Map(source.CreatedOn),
            RevisionDateTime = SpsDateTimeMapper.Map(source.UpdatedOn, source.UpdatedOnSpecified),
            RedispatchDetails = Map(source.RedispatchDetails, context),
            ControlDetails = Map(source.ControlDetails),
            CertifyingOfficerAuthentication = SpsAuthenticationMapper.Map(
                source.CertifyingOfficerSPSAuthentication,
                context
            ),
        };

    private static RedispatchDetails? Map(DocomFollowUpRedispatchDetailsType? source, MappingContext context)
    {
        if (source is null)
            return null;

        return new RedispatchDetails
        {
            RedispatchDateTime = SpsDateTimeMapper.Map(source.RedispatchDateTime, source.RedispatchDateTimeSpecified),
            ExitAuthorityParty = SpsPartyMapper.Map(source.ExitAuthoritySPSParty),
            DestinationCountry = SpsCountryMapper.Map(source.CountryOfDestination, context),
            MeansOfTransport = source.MeansOfTransport?.Select(Map).ToList().NullIfEmpty(),
            PlaceOfDestinationParty = SpsPartyMapper.Map(source.PlaceOfDestinationSPSParty),
        };
    }

    private static MeansOfTransport Map(DocomFollowUpMeansOfTransportType source) =>
        new()
        {
            SpecifiedLogisticsTransportMovement = SpsTransportMovementMapper.Map(source.SPSTransportMovement),
            // TRACES spells the element "InternationalTrasportDocument"; the contract corrects it.
            InternationalTransportDocument = source.InternationalTrasportDocument?.Value,
        };

    private static ControlDetails? Map(DocomFollowUpControlDetailsType? source)
    {
        if (source is null)
            return null;

        return new ControlDetails
        {
            ConsignmentArrivedIndicator = source.ArrivalOfTheConsignmentSpecified
                ? source.ArrivalOfTheConsignment
                : null,
            ConsignmentCompliantIndicator = source.ComplianceOfTheConsignmentSpecified
                ? source.ComplianceOfTheConsignment
                : null,
            IncludedNote = SpsNoteMapper.MapList(source.IncludedSPSNote),
        };
    }
}
