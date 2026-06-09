using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class IntraMapper
{
    internal static DefraUNVTDINTRAProfile Map(EuIntraCertificateType source, MappingContext context) =>
        new()
        {
            ExchangedDocument = SpsExchangedDocumentMapper.Map(source.SPSCertificate.SPSExchangedDocument, context),
            SpecifiedConsignment = SpsConsignmentMapper.Map(source.SPSCertificate.SPSConsignment, context),
            LaboratoryObservationResult = null,
        };

    internal static DefraUNVTDINTRASummaryProfileItem Map(EuIntraCertificateQueryResultType source) =>
        new()
        {
            Id = source.ID,
            Created = source.CreateDateTime,
            Origin = source.CountryOfOrigin?.FirstOrDefault()?.Value ?? string.Empty,
            Updated = source.UpdateDateTime,
        };

    internal static DefraUNVTDINTRASummaryProfile Map(FindEuIntraCertificateResultType source) =>
        new()
        {
            Items = source.EuIntraCertificateResult.Select(Map).ToArray(),
            Offset = source.offset,
            PageSize = source.pageSize,
            HasMore = source.EuIntraCertificateResult.Length == source.pageSize,
        };
}

internal static class EuIntraCertificateTypeExtensions
{
    internal static DefraUNVTDINTRAProfile ToDefraUNVTDINTRAProfile(
        this EuIntraCertificateType source,
        MappingContext context
    ) => IntraMapper.Map(source, context);
}
