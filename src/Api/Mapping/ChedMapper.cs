using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class ChedMapper
{
    internal static DefraUNVTDCHEDProfile Map(ChedCertificateType source, MappingContext context) =>
        new()
        {
            ExchangedDocument = SpsExchangedDocumentMapper.Map(source.SPSCertificate.SPSExchangedDocument, context),
            SpecifiedConsignment = SpsConsignmentMapper.Map(source.SPSCertificate.SPSConsignment, context),
            LaboratoryObservationResult = null
        };

    internal static DefraUNVTDCHEDSummaryProfileItem Map(ChedCertificateQueryResultType source) =>
        new()
        {
            Id = source.ID,
            Created = source.CreateDateTime,
            Origin = source.CountryOfOrigin?.FirstOrDefault()?.Value ?? string.Empty,
            Updated = source.UpdateDateTime,
        };

    internal static DefraUNVTDCHEDSummaryProfile Map(FindChedCertificateResultType source)
    {
        var results = source.ChedCertificateResult ?? [];

        return new()
        {
            Items = results.Select(Map).ToArray(),
            Offset = source.offset,
            PageSize = source.pageSize,
            HasMore = results.Length == source.pageSize,
        };
    }
}
