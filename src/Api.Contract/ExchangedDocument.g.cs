#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record ExchangedDocument
{
    [JsonPropertyName("name")]
    [Description("unece:name is xsd:string in unece-context-D23B.jsonld.")]
    public string? Name { get; init; }

    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("documentTypeCode")]
    [Description("Document type: unece:typeCode is xsd:string in unece-context-D23B.jsonld (use lexical string for numeric codes).")]
    public required string DocumentTypeCode { get; init; }

    [JsonPropertyName("documentStatusCode")]
    [Description("Status code as string per UN vocabulary typing for coded strings.")]
    public string? DocumentStatusCode { get; init; }

    [JsonPropertyName("issueDateTime")]
    public DateTimeOffset? IssueDateTime { get; init; }

    [JsonPropertyName("includedNote")]
    public List<IncludedNote>? IncludedNote { get; init; }

    [JsonPropertyName("referenceDocument")]
    public List<ReferencedDocument>? ReferenceDocument { get; init; }

    [JsonPropertyName("firstSignatoryAuthentication")]
    public Authentication? FirstSignatoryAuthentication { get; init; }

    [JsonPropertyName("secondSignatoryAuthentication")]
    public Authentication? SecondSignatoryAuthentication { get; init; }

    [JsonPropertyName("thirdSignatoryAuthentication")]
    public Authentication? ThirdSignatoryAuthentication { get; init; }

    [JsonPropertyName("fourthSignatoryAuthentication")]
    public Authentication? FourthSignatoryAuthentication { get; init; }
}
