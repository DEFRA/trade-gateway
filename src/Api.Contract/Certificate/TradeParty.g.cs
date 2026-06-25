#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TradeParty
{
    [JsonPropertyName("identifier")]
    [Description("Party identifier (bare string). The sibling urlId names the codelist/register the value is drawn from (e.g. operator_internal_activity_id, veterinary_office_identifier, authority_activity_id, or Defra-side schemes such as uk_transporter_authorisation).")]
    public string? Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the codelist/register the party identifier is drawn from. The gateway translates urlId to/from TRACES schemeId / schemeAgencyId.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("name")]
    [Description("unece:name is xsd:string in unece-context-D23B.jsonld.")]
    public string? Name { get; init; }

    [JsonPropertyName("partyRoleCode")]
    [Description("unece:partyRoleCode as a coded value. value carries the role code; the optional sibling urlId names the codelist it is drawn from.")]
    public CodedValue? PartyRoleCode { get; init; }

    [JsonPropertyName("partyTypeCode")]
    [Description("Party-type code(s) as coded values. A party can carry codes from more than one TRACES list (e.g. COMMERCIAL_TRANSPORTER under operator_activity_type plus TRANSPORTER under classification_section_code); each entry's urlId names the codelist its value is drawn from.")]
    public List<CodedValue>? PartyTypeCode { get; init; }

    [JsonPropertyName("postalAddress")]
    public TradeAddress? PostalAddress { get; init; }

    [JsonPropertyName("definedContact")]
    public List<TradePartyDefinedContactItem>? DefinedContact { get; init; }
}

public partial record TradePartyDefinedContactItem
{
    [JsonPropertyName("personName")]
    public string? PersonName { get; init; }

    [JsonPropertyName("emailURIUniversalCommunication")]
    [Description("Contact email. Additive to the legacy personName-only shape — TRACES SOAP does not carry contact email/phone, but Defra import pre-notifications source these from IPAFFS and need to carry them on consignor / consignee / delivery / carrier / issuer parties.")]
    public string? EmailURIUniversalCommunication { get; init; }

    [JsonPropertyName("telephoneUniversalCommunication")]
    [Description("Contact telephone. Same rationale as emailURIUniversalCommunication.")]
    public string? TelephoneUniversalCommunication { get; init; }
}
