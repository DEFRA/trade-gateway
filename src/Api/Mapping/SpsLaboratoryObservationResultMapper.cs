using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsLaboratoryObservationResultMapper
{
    internal static List<LaboratoryObservationResult>? MapList(
        SPSConsignmentItemLaboratoryTestType[]? source,
        MappingContext context
    ) => source?.Select(s => Map(s, context)).ToList().NullIfEmpty();

    static LaboratoryObservationResult Map(SPSConsignmentItemLaboratoryTestType source, MappingContext context) =>
        new()
        {
            NatureIdCargo = source.NatureIdentificationSPSCargo is { } cargo ? SpsCargoMapper.Map(cargo) : null,
            ProductLaboratoryTest = source
                .ProductSPSLaboratoryTest?.Select(t => Map(t, context))
                .ToList()
                .NullIfEmpty(),
        };

    static ProductLaboratoryTest Map(ProductSPSLaboratoryTestType source, MappingContext context) =>
        new()
        {
            ApplicableProductClassification = source.ProductSPSClassification is { } classification
                ? SpsClassificationMapper.Map(classification, context)
                : null,
            LaboratoryTest = source.SPSLaboratoryTest?.Select(Map).ToList().NullIfEmpty(),
        };

    static LaboratoryTest Map(SPSLaboratoryTestType source) =>
        new()
        {
            Reference = source.Reference,
            TestDescriptor = source.TestDescriptor is { } descriptor ? Map(descriptor) : null,
            TestMotivationCode = source.TestMotivationCode?.Value,
            InspectorConclusionCode = source.InspectorConclusionCode?.Value,
            Analysis = source.Analysys is { Length: > 0 } analyses ? Map(analyses[0]) : null,
        };

    static LaboratoryTestTestDescriptor Map(LaboratoryTestDescriptorType source) =>
        new()
        {
            Id = int.TryParse(source.ID?.Value, out var id) ? id : null,
            Identifier = source.ID?.Value,
            Description = source.Description?.Value,
            CategoryCode = source.CategoryCode?.Value,
        };

    static LaboratoryTestAnalysis Map(SPSLaboratoryTestAnalysisType source) =>
        new()
        {
            AnalysisTypeCode = source.AnalysisTypeCode?.Value,
            SamplingDateTime = SpsDateTimeMapper.Map(source.SamplingDateTime, source.SamplingDateTimeSpecified),
            SampleBatchNumber = int.TryParse(source.SampleBatchNumber, out var batch) ? batch : null,
            NumberOfSamples = int.TryParse(source.NumberOfSamples, out var samples) ? samples : null,
            SampleTypeCode = source.SampleTypeCode?.Value,
            SampleConservationCode = source.SampleConservationCode?.Value,
            Laboratory = SpsPartyMapper.Map(source.LaboratorySPSParty),
            LaboratoryReceiptDateTime = SpsDateTimeMapper.Map(
                source.LaboratoryReceiptDateTime,
                source.LaboratoryReceiptDateTimeSpecified
            ),
            LaboratoryReportDateTime = SpsDateTimeMapper.Map(
                source.LaboratoryReportDateTime,
                source.LaboratoryReportDateTimeSpecified
            ),
            LaboratoryTestMethod = source.LaboratoryTestMethod,
            LaboratoryResults = source.LaboratoryResults,
            LaboratoryConclusionCode = source.LaboratoryConclusionCode?.Value,
        };
}
