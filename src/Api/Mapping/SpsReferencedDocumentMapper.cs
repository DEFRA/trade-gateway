using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsReferencedDocumentMapper
{
    internal static ReferencedDocument Map(SPSReferencedDocumentType source) =>
        new()
        {
            TypeCode = source.TypeCode?.Value.XmlEnumCode(),
            RelationshipTypeCode = source.RelationshipTypeCode?.Value.XmlEnumCode(),
            Identifier = source.ID?.Value,
            AttachmentBinaryObject = null,
            Information = source.Information?.Value is { } info ? [info] : null,
        };
}
