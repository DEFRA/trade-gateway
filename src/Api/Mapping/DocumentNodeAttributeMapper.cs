using Trade.Gateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class DocumentNodeAttributeMapper
{
    internal static DocumentNodeAttribute Map(SelectableDocumentLinkNodeAttribute source) =>
        new()
        {
            Key = source.id,
            Description = source.Description.Value,
            DocumentLinkTypes = source.DocumentTypeValue?.Select(Map).ToList()
        };

    internal static DocumentNodeAttributeValue Map(SelectableDocumentLinkNodeAttributeValue source) =>
        new () { DocumentType = source.Value, LinkType = source.linkType };
}
