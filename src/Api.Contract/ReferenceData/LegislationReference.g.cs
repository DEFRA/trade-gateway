#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Defra.TradeGateway.Api.Contract.ReferenceData;
public partial record LegislationReference
{
    [JsonPropertyName("legislationId")]
    public required int LegislationId { get; init; }

    [JsonPropertyName("celexIdentifiers")]
    public List<string>? CelexIdentifiers { get; init; }

    [JsonPropertyName("certificateModels")]
    public List<CertificateModelReference>? CertificateModels { get; init; }

    [JsonPropertyName("originCountries")]
    public List<string>? OriginCountries { get; init; }

    [JsonPropertyName("destinationCountries")]
    public List<string>? DestinationCountries { get; init; }

    [JsonPropertyName("originClassificationSections")]
    public List<ClassificationSection>? OriginClassificationSections { get; init; }

    [JsonPropertyName("destinationClassificationSections")]
    public List<ClassificationSection>? DestinationClassificationSections { get; init; }
}
