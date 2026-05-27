#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record LaboratoryObservationResult
{
    [JsonPropertyName("natureIdCargo")]
    public CargoNature? NatureIdCargo { get; init; }

    [JsonPropertyName("productLaboratoryTest")]
    public List<ProductLaboratoryTest>? ProductLaboratoryTest { get; init; }
}
