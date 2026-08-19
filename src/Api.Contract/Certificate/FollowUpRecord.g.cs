#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record FollowUpRecord
{
    [JsonPropertyName("creationDateTime")]
    [Description("When the follow-up record was created (TRACES CreatedOn, unece:creationDateTime).")]
    public DateTimeOffset? CreationDateTime { get; init; }

    [JsonPropertyName("revisionDateTime")]
    [Description("When the follow-up record was last revised (TRACES UpdatedOn, unece:revisionDateTime). Same term as the certificate's revisionDateTime, scoped to this record.")]
    public DateTimeOffset? RevisionDateTime { get; init; }

    [JsonPropertyName("redispatchDetails")]
    public RedispatchDetails? RedispatchDetails { get; init; }

    [JsonPropertyName("controlDetails")]
    public ControlDetails? ControlDetails { get; init; }

    [JsonPropertyName("certifyingOfficerAuthentication")]
    [Description("The officer who signed off this follow-up action (TRACES CertifyingOfficerSPSAuthentication). Same Authentication shape as the certificate signatories: typeCode carries the action per UNCL9417, provider carries the authority and the named official.")]
    public Authentication? CertifyingOfficerAuthentication { get; init; }
}
