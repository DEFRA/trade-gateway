using TracesNT.WebServices;

namespace Api.Attachments;

public static class ChedAttachmentLocator
{
    /// <summary>
    /// TracesNT identifies attachments by a uri of the form "uri:documentid:{documentId}", with the
    /// filename alongside it on the same BinaryObjectType. They hang off the exchanged document and
    /// off each trade line item.
    /// </summary>
    public static string? FindFileName(SPSCertificateType certificate, long attachmentId)
    {
        var tradeLineItemDocuments =
            certificate
                .SPSConsignment?.IncludedSPSConsignmentItem?.SelectMany(item => item.IncludedSPSTradeLineItem ?? [])
                .SelectMany(lineItem => lineItem.ReferenceSPSReferencedDocument ?? [])
            ?? [];

        return (certificate.SPSExchangedDocument?.ReferenceSPSReferencedDocument ?? [])
            .Concat(tradeLineItemDocuments)
            .SelectMany(document => document.AttachmentBinaryObject ?? [])
            .FirstOrDefault(binaryObject => binaryObject.uri == $"uri:documentid:{attachmentId}")
            ?.filename;
    }
}
