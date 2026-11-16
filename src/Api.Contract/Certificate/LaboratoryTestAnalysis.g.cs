#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LaboratoryTestAnalysis
{
    [JsonPropertyName("analysisTypeCode")]
    public string? AnalysisTypeCode { get; init; }

    [JsonPropertyName("samplingDateTime")]
    public DateTimeOffset? SamplingDateTime { get; init; }

    [JsonPropertyName("sampleBatchNumber")]
    public int? SampleBatchNumber { get; init; }

    [JsonPropertyName("numberOfSamples")]
    public int? NumberOfSamples { get; init; }

    [JsonPropertyName("sampleTypeCode")]
    public string? SampleTypeCode { get; init; }

    [JsonPropertyName("sampleConservationCode")]
    public string? SampleConservationCode { get; init; }

    [JsonPropertyName("laboratory")]
    public TradeParty? Laboratory { get; init; }

    [JsonPropertyName("laboratoryReceiptDateTime")]
    public DateTimeOffset? LaboratoryReceiptDateTime { get; init; }

    [JsonPropertyName("laboratoryReportDateTime")]
    public DateTimeOffset? LaboratoryReportDateTime { get; init; }

    [JsonPropertyName("laboratoryTestMethod")]
    public string? LaboratoryTestMethod { get; init; }

    [JsonPropertyName("laboratoryResults")]
    public string? LaboratoryResults { get; init; }

    [JsonPropertyName("laboratoryConclusionCode")]
    public string? LaboratoryConclusionCode { get; init; }
}
