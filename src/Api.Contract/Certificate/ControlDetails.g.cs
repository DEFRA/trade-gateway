#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record ControlDetails
{
    [JsonPropertyName("consignmentArrivedIndicator")]
    [Description("Whether the consignment arrived at its declared destination (TRACES ArrivalOfTheConsignment).")]
    public bool? ConsignmentArrivedIndicator { get; init; }

    [JsonPropertyName("consignmentCompliantIndicator")]
    [Description("Whether the consignment was found compliant at the control (TRACES ComplianceOfTheConsignment). When false, the reasons are carried as includedNote entries with subject code REASON_OF_NON_COMPLIANCE.")]
    public bool? ConsignmentCompliantIndicator { get; init; }

    [JsonPropertyName("includedNote")]
    [Description("Coded control detail. Subject codes are drawn from the TRACES ched_follow_up_note_subject_code list — TYPE_OF_CONTROL (contentCode from docom_follow_up_type_of_control) and REASON_OF_NON_COMPLIANCE (contentCode from docom_follow_up_reason_of_non_compliance, which can carry several codes on one note).")]
    public List<IncludedNote>? IncludedNote { get; init; }
}
