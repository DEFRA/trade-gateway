using Defra.TradeGateway.Api.Contract.ReferenceData;
using ContractCertificateModelReference = Defra.TradeGateway.Api.Contract.ReferenceData.CertificateModelReference;
using ContractLegislationReference = Defra.TradeGateway.Api.Contract.ReferenceData.LegislationReference;
using SoapCertificateModelReference = TracesNT.WebServices.CertificateModelReference;
using SoapClassificationSectionReference = TracesNT.WebServices.ClassificationSectionReference;
using SoapLegislationReference = TracesNT.WebServices.LegislationReference;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class LegislationAttributeMapper
{
    internal static LegislationAttribute Map(LegislationNodeAttribute source) =>
        new()
        {
            Key = source.id,
            Description = source.Description.Value,
            Legislation = [Map(source.LegislationReference)],
        };

    private static ContractLegislationReference Map(SoapLegislationReference source) =>
        new()
        {
            LegislationId = checked((int)source.legislationId),
            CelexIdentifiers = source.CelexIdentifier?.Select(GetIdValue).ToList().NullIfEmpty(),
            CertificateModels = source.CertificateModel
                ?.Select(Map)
                .ToList()
                .NullIfEmpty(),
            OriginCountries = source.OriginCountry?.Select(GetIdValue).ToList().NullIfEmpty(),
            DestinationCountries = source.DestinationCountry
                ?.Select(GetIdValue)
                .ToList()
                .NullIfEmpty(),
            OriginClassificationSections = source.OriginClassificationSection
                ?.Select(Map)
                .ToList()
                .NullIfEmpty(),
            DestinationClassificationSections = source.DestinationClassificationSection
                ?.Select(Map)
                .ToList()
                .NullIfEmpty(),
        };

    private static ContractCertificateModelReference Map(SoapCertificateModelReference source) =>
        new()
        {
            ModelId = checked((int)source.modelId),
            ShortTitle = source.ShortTitle?.Value,
            LongTitle = source.LongTitle?.Value,
            CreatedOn = source.createdOn.ToUniversalTime(),
            UpdatedOn = source.updatedOnSpecified ? source.updatedOn.ToUniversalTime() : null,
        };

    private static ClassificationSection Map(SoapClassificationSectionReference source) =>
        ClassificationSectionMapper.Map(source);

    private static string GetIdValue(IDType source) => source.Value;
}
