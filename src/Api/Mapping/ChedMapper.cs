using System.Globalization;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class ChedMapper
{
    private const string LastUpdateSubjectCode = "LAST_UPDATE_DATETIME";

    internal static DefraUNVTDCHEDProfile Map(ChedCertificateType source, MappingContext context)
    {
        var exchangedDocument = SpsExchangedDocumentMapper.Map(source.SPSCertificate.SPSExchangedDocument, context);
        return new()
        {
            ExchangedDocument = exchangedDocument,
            SpecifiedConsignment = SpsConsignmentMapper.Map(source.SPSCertificate.SPSConsignment, context),
            LaboratoryObservationResult = null,
            LastUpdated = exchangedDocument.GetLatestLastUpdateDateTime(),
        };
    }

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

    internal static DateTimeOffset? GetLatestLastUpdateDateTime(this ExchangedDocument exchangedDocument)
    {
        var notes = exchangedDocument.IncludedNote;
        if (notes == null || notes.Count == 0)
        {
            return null;
        }

        return notes
            .Where(n => string.Equals(n.SubjectCode?.Value, LastUpdateSubjectCode, StringComparison.OrdinalIgnoreCase))
            .SelectMany(n => n.Content ?? Enumerable.Empty<string>())
            .Select(c =>
            {
                if (DateTimeOffset.TryParse(c, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                {
                    return (DateTimeOffset?)dto;
                }

                return null;
            })
            .Where(d => d.HasValue)
            .Max();
    }
}
