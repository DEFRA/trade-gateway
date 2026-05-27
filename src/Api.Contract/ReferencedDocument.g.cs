#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record ReferencedDocument
{
    [JsonPropertyName("typeCode")]
    [Description("unece:typeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("relationshipTypeCode")]
    [Description("unece:relationshipTypeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? RelationshipTypeCode { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("attachmentBinaryObject")]
    public AttachmentBinaryObject? AttachmentBinaryObject { get; init; }

    [JsonPropertyName("information")]
    public List<string>? Information { get; init; }
}
