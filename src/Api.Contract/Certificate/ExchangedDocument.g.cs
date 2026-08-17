#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record ExchangedDocument
{
    [JsonPropertyName("name")]
    [Description("unece:name is xsd:string in unece-context-D23B.jsonld.")]
    public string? Name { get; init; }

    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("traderAssignedId")]
    [Description("Optional trader-entered reference. BSP/D23B canonical slot (unece:traderAssignedId, xsd:string) for an importer's own internal reference number against the consignment / pre-notification.")]
    public string? TraderAssignedId { get; init; }

    [JsonPropertyName("documentTypeCode")]
    [Description("Document type per UNTDID 1001 (e.g. 636 CHED, 666/856 INTRA). Optional at core level; profile schemas constrain by const/enum.")]
    public string? DocumentTypeCode { get; init; }

    [JsonPropertyName("documentStatusCode")]
    [Description("Status code as string per UN vocabulary typing for coded strings.")]
    public string? DocumentStatusCode { get; init; }

    [JsonPropertyName("notificationStatusCode")]
    [Description("Defra-internal workflow status of this notification. Codelist binding declared in the Defra profile vocabulary.")]
    public string? NotificationStatusCode { get; init; }

    [JsonPropertyName("versionId")]
    [Description("Document revision number. V1 on first submission, increments on each subsequent re-submission. Distinct from the envelope's aggregateVersion (event sequence) and the schema's structural version.")]
    public int? VersionId { get; init; }

    [JsonPropertyName("functionCode")]
    [Description("UNTDID 1225 message function: 9 (Original), 5 (Replace), 4 (Change), 1 (Cancellation), 3 (Deletion). Tells the consumer what the message does to its view of the document.")]
    public string? FunctionCode { get; init; }

    [JsonPropertyName("issueDateTime")]
    public DateTimeOffset? IssueDateTime { get; init; }

    [JsonPropertyName("issuer")]
    [Description("The party responsible for issuing this document. For TRACES CHED and Defra import pre-notifications, this carries the responsible-person organisation with a named contact.")]
    public TradeParty? Issuer { get; init; }

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
