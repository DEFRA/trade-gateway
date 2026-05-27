using Trade.Gateway.Api.Contract;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class IntraMapper
{
    internal static DefraUNVTDINTRAProfile Map(EuIntraCertificateType source, MappingContext context) => new()
    {
        ExchangedDocument = SpsExchangedDocumentMapper.Map(source.SPSCertificate.SPSExchangedDocument, context),
        SpecifiedConsignment = [SpsConsignmentMapper.Map(source.SPSCertificate.SPSConsignment, context)],
        LaboratoryObservationResult = null
    };
}

internal static class EuIntraCertificateTypeExtensions
{
    internal static DefraUNVTDINTRAProfile ToDefraUNVTDINTRAProfile(this EuIntraCertificateType source, MappingContext context)
        => IntraMapper.Map(source, context);
}
