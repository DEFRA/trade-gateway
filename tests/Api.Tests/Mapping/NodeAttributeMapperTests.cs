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
            id = "columns",
            Description = new TextType { Value = "Columns" },
            DescriptorColumnValue =
            [
                new DescriptorColumnNodeAttributeValue { id = "TAXON_ID" },
                new DescriptorColumnNodeAttributeValue { id = "" },
                new DescriptorColumnNodeAttributeValue { id = "QUANTITY" },
            ],
        };

        var result = NodeAttributeMapper.Map(source);

        result.Value.Should().NotBeNull();
        result.Value!.Value.EnumerateArray().Select(v => v.GetString()).Should().Equal("TAXON_ID", "QUANTITY");
    }

    [Fact]
    public void Map_UnknownAttribute_ReturnsNullValue()
    {
        var source = new UnknownNodeAttribute { Description = new TextType { Value = "Unknown" } };

        NodeAttributeMapper.Map(source).Value.Should().BeNull();
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
