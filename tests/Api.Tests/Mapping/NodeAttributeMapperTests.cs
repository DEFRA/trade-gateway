using System.Text.Json;
using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class NodeAttributeMapperTests
{
    [Fact]
    public void Map_IntegerNodeAttribute_SerializesInteger()
    {
        var source = new IntegerNodeAttribute
        {
            id = "count",
            Description = new TextType { Value = "Count" },
            IntegerValue = "42",
        };

        var result = NodeAttributeMapper.Map(source);

        result.Value!.Value.GetInt32().Should().Be(42);
    }

    [Fact]
    public void Map_DescriptorColumnAttribute_SerializesFilteredArray()
    {
        var source = new DescriptorColumnNodeAttribute
        {
            id = "AVAILABLE_CHED_DESCRIPTOR_COLUMNS",
            Description = new TextType { Value = "Descriptor Columns" },
            DescriptorColumnValue =
            [
                new DescriptorColumnNodeAttributeValue
                {
                    id = "INVASIVE_TAXON_ID",
                    Cardinality = CardinalityType.MANDATORY,
                    Description = new TextType { Value = "Species / EPPO code&lt;br/&gt;Invasive / Allowed" },
                    columnType = DescriptorColumnType.TAXON_INVASIVE,
                },
                new DescriptorColumnNodeAttributeValue
                {
                    id = "QUANTITY",
                    Cardinality = CardinalityType.OPTIONAL,
                    Description = new TextType { Value = "Quantity" },
                    columnType = DescriptorColumnType.QUANTITY,
                },
            ],
        };

        var result = DescriptorColumnNodeAttributeMapper.Map(source);

        result.Description.Should().Be("Descriptor Columns");
        result.Key.Should().Be("AVAILABLE_CHED_DESCRIPTOR_COLUMNS");
        result.DescriptorColumns.Count.Should().Be(2);

        result.DescriptorColumns[0].Key.Should().Be("INVASIVE_TAXON_ID");
        result.DescriptorColumns[0].Label.Should().Be("Species / EPPO code&lt;br/&gt;Invasive / Allowed");
        result.DescriptorColumns[0].ColumnType.Should().Be("TAXON_INVASIVE");
        result.DescriptorColumns[0].Cardinality.Should().Be("MANDATORY");

        result.DescriptorColumns[1].Key.Should().Be("QUANTITY");
        result.DescriptorColumns[1].Label.Should().Be("Quantity");
        result.DescriptorColumns[1].ColumnType.Should().Be("QUANTITY");
        result.DescriptorColumns[1].Cardinality.Should().Be("OPTIONAL");
    }

    [Fact]
    public void Map_UnknownAttribute_ReturnsNullValue()
    {
        var source = new UnknownNodeAttribute { Description = new TextType { Value = "Unknown" } };

        NodeAttributeMapper.Map(source).Value.Should().BeNull();
    }

    [Fact]
    public void Map_DescriptorColumnNodeAttribute_ThrowsNotSupported()
    {
        var source = new DescriptorColumnNodeAttribute
        {
            id = "columns",
            Description = new TextType { Value = "Columns" },
            DescriptorColumnValue =
            [
                new DescriptorColumnNodeAttributeValue { id = "TAXON_ID" },
                new DescriptorColumnNodeAttributeValue { id = "" },
                new DescriptorColumnNodeAttributeValue { id = "QUANTITY" },
            ],
        };

        var ex = Assert.Throws<NotSupportedException>(() => NodeAttributeMapper.Map(source));
        ex.Message.Should().Contain(nameof(DescriptorColumnNodeAttributeMapper));
    }

    [Fact]
    public void Map_SelectableDocumentLinkNodeAttribute_ThrowsNotSupported()
    {
        var source = new SelectableDocumentLinkNodeAttribute
        {
            id = "SELECTABLE_DOCUMENT_LINKS",
            Description = new TextType { Value = "Selectable document links" },
            DocumentTypeValue =
            [
                new SelectableDocumentLinkNodeAttributeValue { Value = "EU_INTRA", linkType = "ATTACHED_TO" },
            ],
        };

        var ex = Assert.Throws<NotSupportedException>(() => NodeAttributeMapper.Map(source));
        ex.Message.Should().Contain(nameof(DocumentNodeAttributeMapper));
    }

    [Fact]
    public void Map_LegislationNodeAttribute_ThrowsNotSupported()
    {
        var source = new LegislationNodeAttribute
        {
            id = "LEGISLATION_POSSIBLE_VALUES",
            Description = new TextType { Value = "Legislation" },
        };

        var ex = Assert.Throws<NotSupportedException>(() => NodeAttributeMapper.Map(source));
        ex.Message.Should().Contain(nameof(LegislationAttributeMapper));
    }

    [Fact]
    public void Map_ClassificationSectionNodeAttribute_ThrowsNotSupported()
    {
        var source = new ClassificationSectionNodeAttribute
        {
            id = "CONSIGNEE_CLASSIFICATION_SECTIONS",
            Description = new TextType { Value = "Consignee sections" },
        };

        var ex = Assert.Throws<NotSupportedException>(() => NodeAttributeMapper.Map(source));
        ex.Message.Should().Contain(nameof(ClassificationSectionNodeAttributeMapper));
    }

    [Fact]
    public void Map_TaxonNodeAttribute_ThrowsNotSupported()
    {
        var source = new TaxonNodeAttribute
        {
            id = "TAXON_POSSIBLE_VALUES",
            Description = new TextType { Value = "Taxons" },
        };

        var ex = Assert.Throws<NotSupportedException>(() => NodeAttributeMapper.Map(source));
        ex.Message.Should().Contain(nameof(TaxonMapper));
    }

    public class UnknownNodeAttribute : AbstractNodeAttribute;
}
