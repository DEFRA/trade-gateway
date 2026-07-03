#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;
public partial record CertificateModelReference
{
    [JsonPropertyName("modelId")]
    public required int ModelId { get; init; }

    [JsonPropertyName("shortTitle")]
    public string? ShortTitle { get; init; }

    [JsonPropertyName("longTitle")]
    public string? LongTitle { get; init; }

    [JsonPropertyName("createdOn")]
    public DateTimeOffset? CreatedOn { get; init; }

    [JsonPropertyName("updatedOn")]
    public DateTimeOffset? UpdatedOn { get; init; }
}
