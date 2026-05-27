#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record ProductLaboratoryTest
{
    [JsonPropertyName("applicableProductClassification")]
    public ProductClassification? ApplicableProductClassification { get; init; }

    [JsonPropertyName("laboratoryTest")]
    public List<object>? LaboratoryTest { get; init; }
}
