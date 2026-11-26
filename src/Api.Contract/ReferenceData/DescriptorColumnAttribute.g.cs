#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;
public partial record DescriptorColumnAttribute
{
    [JsonPropertyName("key")]
    [Description("Identifier for this descriptor column attribute")]
    public required string Key { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("descriptorColumns")]
    public required List<DescriptorColumn> DescriptorColumns { get; init; }
}
