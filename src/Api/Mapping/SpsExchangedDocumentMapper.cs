using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsExchangedDocumentMapper
{
    internal static ExchangedDocument Map(SPSExchangedDocumentType source, MappingContext context) =>
        new()
        {
            Name = source.Name.ForLanguage(context.LanguageCode),
            Identifier = source.ID?.Value ?? string.Empty,
            DocumentTypeCode = source.TypeCode?.Value.XmlEnumCode() ?? string.Empty,
            DocumentStatusCode = source.StatusCode?.Value.XmlEnumCode(),
            Issuer = SpsPartyMapper.Map(source.IssuerSPSParty),
            IssueDateTime = SpsDateTimeMapper.Map(source.IssueDateTime),
            IncludedNote = SpsNoteMapper.MapList(source.IncludedSPSNote),
            ReferenceDocument = source
                .ReferenceSPSReferencedDocument?.Select(SpsReferencedDocumentMapper.Map)
                .ToList()
                .NullIfEmpty(),
            FirstSignatoryAuthentication = SpsAuthenticationMapper.MapByCode(
                source.SignatorySPSAuthentication,
                GovernmentActionCode.Inspection,
                context
            ),
            SecondSignatoryAuthentication = SpsAuthenticationMapper.MapByCode(
                source.SignatorySPSAuthentication,
                GovernmentActionCode.Clearance,
                context
            ),
            ThirdSignatoryAuthentication = SpsAuthenticationMapper.MapByCode(
                source.SignatorySPSAuthentication,
                GovernmentActionCode.ContainerInspection,
                context
            ),
        };
}
