#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record ProductLaboratoryTest
{
    [JsonPropertyName("applicableProductClassification")]
    public ApplicableClassification? ApplicableProductClassification { get; init; }

    [JsonPropertyName("laboratoryTest")]
    public List<object>? LaboratoryTest { get; init; }
}
