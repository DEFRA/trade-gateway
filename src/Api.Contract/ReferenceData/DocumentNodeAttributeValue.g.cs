#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Defra.TradeGateway.Api.Contract.ReferenceData;
public partial record DocumentNodeAttributeValue
{
    [JsonPropertyName("documentType")]
    public required string DocumentType { get; init; }

    [JsonPropertyName("linkType")]
    public required string LinkType { get; init; }
}
