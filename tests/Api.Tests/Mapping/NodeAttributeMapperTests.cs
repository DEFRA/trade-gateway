using System.Text.Json;
using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class NodeAttributeMapperTests
{
    [Fact]
    public void Map_UsesFallbackKeyAndMapsBooleanValue()
    {
        var source = new BooleanNodeAttribute
        {
            mappedId = "mapped-key",
            Description = new TextType { Value = "Boolean attribute" },
            BooleanValue = true,
        };

        var result = NodeAttributeMapper.Map(source);

        result.Key.Should().Be("mapped-key");
        result.Description.Should().Be("Boolean attribute");
        result.Value.Should().NotBeNull();
        result.Value!.Value.ValueKind.Should().Be(JsonValueKind.True);
    }

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

    public class UnknownNodeAttribute : AbstractNodeAttribute;
}
