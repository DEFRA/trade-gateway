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
}
