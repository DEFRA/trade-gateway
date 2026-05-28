#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Defra.TradeGateway.Api.Contract.ReferenceData;
public partial record ClassificationTreeSummary
{
    [JsonPropertyName("treeId")]
    public required string TreeId { get; init; }

    [JsonPropertyName("treeName")]
    public required string TreeName { get; init; }
}
