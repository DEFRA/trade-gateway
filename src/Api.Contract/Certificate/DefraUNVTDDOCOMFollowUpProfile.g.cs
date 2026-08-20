#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record DefraUNVTDDOCOMFollowUpProfile
{
    [JsonPropertyName("$model")]
    [ConstValue("defra/certificate-internal/1")]
    public string Model { get; init; } = "defra/certificate-internal/1";

    [JsonPropertyName("$type")]
    [ConstValue("docom-followup")]
    public string Type { get; init; } = "docom-followup";

    [JsonPropertyName("certificateIdentifier")]
    [Description("The identifier of the DOCOM certificate these follow-up records belong to — matches exchangedDocument.identifier on the certificate payload (e.g. DOCOM.ES.2026.0000001).")]
    public required string CertificateIdentifier { get; init; }

    [JsonPropertyName("followUp")]
    [Description("Follow-up records in the order TRACES returned them. TRACES emits one record per follow-up action, so a certificate accumulates records over its life; each record carries redispatch details, control outcomes, or both.")]
    public required List<FollowUpRecord> FollowUp { get; init; }
}
