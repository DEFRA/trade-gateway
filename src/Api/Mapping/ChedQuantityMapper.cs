using System.Globalization;
using Trade.Gateway.Api.Contract.Customs;
using TracesNT.WebServices;
using ContractCommodityCode = Trade.Gateway.Api.Contract.Customs.CommodityCode;

namespace Api.Mapping;

/// <summary>
/// Projects the customs quantity-management summary onto the customs contracts.
/// </summary>
/// <remarks>
/// Takes no <see cref="MappingContext"/> on purpose: customs quantity data carries no localised text,
/// and an unused parameter fails the build under Sonar S1172.
/// </remarks>
internal static class ChedQuantityMapper
{
    internal static ChedQuantityLedger MapLedger(QuantityManagementCommoditySummaryEnhanced4ChedR51Type summary) =>
        new()
        {
            Available = [.. (summary.AvailableQuantity ?? []).Where(q => q is not null).Select(MapAvailable)],
            Allocations = new QuantityAllocations
            {
                Reserved = MapAllocations(summary.ReservedQuantity),
                Consumed = MapAllocations(summary.ConsumedQuantity),
            },
        };

    private static AllocatedCommodityQuantity[] MapAllocations(
        AllocatedProductQuantityByCustomsOfficeEnhanced4ChedR51Type[]? source
    ) => [.. (source ?? []).Where(a => a is not null).Select(MapAllocated)];

    private static AvailableCommodityQuantity MapAvailable(ProductQuantityEnhancedPlusPlusType source) =>
        new()
        {
            CommodityCode = MapCommodityCode(
                source.CommodityCode?.HarmonizedSystemSubheadingcode,
                source.CommodityCode?.CombinedNomenclatureCode,
                source.CommodityCode?.TARICCode
            ),
            CertificateLineNumber = ParseInteger(source.SwSupportingDocument?.CertificateLineNumber),
            // Null only when the whole supporting document is absent. See MapUnitOfMeasure.
            UnitOfMeasure = source.SwSupportingDocument is null
                ? null
                : MapUnitOfMeasure(source.SwSupportingDocument.UnitOfMeasure),
            Quantity = source.SwSupportingDocument?.Quantity ?? 0m,
        };

    private static AllocatedCommodityQuantity MapAllocated(
        AllocatedProductQuantityByCustomsOfficeEnhanced4ChedR51Type source
    ) =>
        new()
        {
            GoodsItemNumber = ParseInteger(source.GoodsItemNumber),
            CommodityCode = MapCommodityCode(
                source.CommodityCode?.HarmonizedSystemSubheadingcode,
                source.CommodityCode?.CombinedNomenclatureCode,
                source.CommodityCode?.TARICCode
            ),
            CertificateLineNumber = ParseInteger(source.SwSupportingDocument?.CertificateLineNumber),
            UnitOfMeasure = source.SwSupportingDocument is null
                ? null
                : MapUnitOfMeasure(source.SwSupportingDocument.UnitOfMeasure),
            Quantity = source.SwSupportingDocument?.Quantity?.Value ?? 0m,
            TechnicalRoundingQuantity = source.SwSupportingDocument?.Quantity is
            { TechnicalRoundingQuantitySpecified: true } quantity
                ? quantity.TechnicalRoundingQuantity
                : null,
            EventDateTime = source.EventDateTimeSpecified ? ToOffset(source.EventDateTime) : null,
            CustomsOffice = source.CompetentCustomsOffice?.ReferenceNumber,
            DeclarationReference = MapDeclarationReference(source.Item, source.ItemElementName),
        };

    /// <summary>
    /// <c>UniversalUnitOfMeasureType</c> has no <c>Specified</c> companion and its first member is
    /// <c>TNE</c>, so an omitted <c>UnitOfMeasure</c> element deserialises to tonnes rather than to
    /// nothing. That cannot be corrected here without editing generated code — it is settled by the
    /// element always being present upstream. <c>ChedQuantityMapperTests</c> documents the default.
    /// </summary>
    private static string MapUnitOfMeasure(UniversalUnitOfMeasureType unitOfMeasure) => unitOfMeasure.ToString();

    private static ContractCommodityCode? MapCommodityCode(
        string? harmonizedSystemSubheadingCode,
        string? combinedNomenclatureCode,
        string? taricCode
    ) =>
        harmonizedSystemSubheadingCode is null && combinedNomenclatureCode is null && taricCode is null
            ? null
            : new ContractCommodityCode
            {
                HarmonizedSystemSubheadingCode = harmonizedSystemSubheadingCode,
                CombinedNomenclatureCode = combinedNomenclatureCode,
                TaricCode = taricCode,
            };

    /// <summary>
    /// The choice element is only an MRN when TracesNT said so. Both enums default to <c>LRN</c> at
    /// index 0, so a present value with no discriminator is read as the LRN it claims to be.
    /// </summary>
    private static DeclarationReference? MapDeclarationReference(string? item, ItemChoiceType2 itemElementName)
    {
        if (string.IsNullOrEmpty(item))
            return null;

        var type =
            itemElementName == ItemChoiceType2.MRN ? DeclarationReferenceType.Mrn : DeclarationReferenceType.Lrn;

        return new DeclarationReference { Type = type, Value = item };
    }

    /// <summary>
    /// <c>xs:integer</c> is unbounded, so svcutil surfaces these as strings. A value that does not
    /// fit an <c>int</c> maps to <c>null</c> rather than throwing — one odd line must not fail the
    /// whole read.
    /// </summary>
    private static int? ParseInteger(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static DateTimeOffset ToOffset(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value);
}
