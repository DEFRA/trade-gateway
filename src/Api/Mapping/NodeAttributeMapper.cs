using System.Globalization;
using System.Text.Json;
using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class NodeAttributeMapper
{
    internal static NodeAttribute Map(AbstractNodeAttribute source) =>
        new()
        {
            Key = source.id,
            Description = source.Description?.Value,
            Value = MapValue(source),
        };

    private static JsonElement? MapValue(AbstractNodeAttribute source) =>
        source switch
        {
            BooleanNodeAttribute booleanAttribute => JsonSerializer.SerializeToElement(
                booleanAttribute.BooleanValue
            ),
            IntegerNodeAttribute integerAttribute => SerializeIntegerValue(
                integerAttribute.IntegerValue
            ),
            IntegerRangeNodeAttribute integerRangeAttribute => SerializeStringArray(
                [integerRangeAttribute.Min, integerRangeAttribute.Max]
            ),
            DoubleRangeNodeAttribute doubleRangeAttribute => SerializeDoubleRange(
                doubleRangeAttribute
            ),
            EnumSingleNodeAttribute enumSingleAttribute => SerializeStringValue(
                GetIdValue(enumSingleAttribute.EnumValue)
            ),
            EnumCollectionNodeAttribute enumCollectionAttribute => SerializeStringArray(
                enumCollectionAttribute.EnumValue,
                GetIdValue
            ),
            FieldAccessNodeAttribute fieldAccessAttribute => SerializeEnumValue(
                fieldAccessAttribute.FieldAccessValue
            ),
            MandatoryNotApplicableNodeAttribute mandatoryAttribute => SerializeEnumValue(
                mandatoryAttribute.MandatoryNotApplicableValue
            ),
            CardinalityNodeAttribute cardinalityAttribute => SerializeEnumValue(
                cardinalityAttribute.CardinalityValue
            ),
            AllowedNodeAttribute allowedAttribute => SerializeEnumValue(
                allowedAttribute.AllowedValue
            ),
            DescriptorColumnNodeAttribute descriptorColumnAttribute => SerializeStringArray(
                descriptorColumnAttribute.DescriptorColumnValue,
                value => value.id
            ),
            SelectableDocumentLinkNodeAttribute _ => throw new NotSupportedException($"Use {nameof(DocumentNodeAttributeMapper)}"),
            LegislationNodeAttribute _ => throw new NotSupportedException($"Use {nameof(LegislationAttributeMapper)}"),
            ClassificationSectionNodeAttribute _ => throw new NotSupportedException($"Use {nameof(ClassificationSectionNodeAttributeMapper)}"),
            TaxonNodeAttribute _ => throw new NotSupportedException($"Use {nameof(TaxonMapper)}"),
            _ => null,
        };

    private static JsonElement? SerializeIntegerValue(string? value) =>
        int.TryParse(value, out var integerValue)
            ? JsonSerializer.SerializeToElement(integerValue)
            : SerializeStringValue(value);

    private static JsonElement? SerializeDoubleRange(DoubleRangeNodeAttribute source) =>
        SerializeStringArray(
            [
                source.MinSpecified ? source.Min.ToString(CultureInfo.InvariantCulture) : null,
                source.MaxSpecified ? source.Max.ToString(CultureInfo.InvariantCulture) : null,
            ]
        );

    private static JsonElement? SerializeEnumValue<T>(T value)
        where T : struct, Enum => SerializeStringValue(value.ToString());

    private static JsonElement? SerializeStringValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.SerializeToElement(value);

    private static JsonElement? SerializeStringArray<T>(
        IEnumerable<T>? values,
        Func<T, string?> selector
    ) => SerializeStringArray(values?.Select(selector));

    private static JsonElement? SerializeStringArray(IEnumerable<string?>? values)
    {
        var materialized = values
            ?.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        return materialized is { Length: > 0 }
            ? JsonSerializer.SerializeToElement(materialized)
            : null;
    }

    private static string? GetIdValue(IDType? source) => source?.Value;
}
