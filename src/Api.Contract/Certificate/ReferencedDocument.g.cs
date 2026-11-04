#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record ReferencedDocument
{
    [JsonPropertyName("typeCode")]
    [Description("Document type per UNTDID 1001 (e.g. 853 Veterinary certificate, 636 Health certificate, 916 Related document, 705 Bill of lading).")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("urlId")]
    [Description("Codelist URI qualifying `typeCode` - the system that defines the code. Here `urlId` qualifies `typeCode`; on `LogisticsLocation` the sibling `urlId` qualifies `identifier`, so a consumer must not assume one rule across types.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("relationshipTypeCode")]
    [Description("Role this referenced document plays per UNTDID 1153.")]
    public string? RelationshipTypeCode { get; init; }

    [JsonPropertyName("identifier")]
    [Description("Reference number assigned by the issuing authority.")]
    public string? Identifier { get; init; }

    [JsonPropertyName("issueDateTime")]
    [Description("Issue date of the referenced document (ISO 8601 YYYY-MM-DD).")]
    public DateOnly? IssueDateTime { get; init; }

    [JsonPropertyName("attachmentBinaryObject")]
    [Description("Attachment for the referenced document, carried by reference: a link to the stored file (`uri`) with `filename` and `mimeCode` alongside. File content is never embedded.")]
    public AttachmentBinaryObject? AttachmentBinaryObject { get; init; }

    [JsonPropertyName("information")]
    public List<string>? Information { get; init; }
}
