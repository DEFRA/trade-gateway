using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsTradeLineItemMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_SequenceNumeric_MapsFromDecimal()
    {
        var source = new SPSTradeLineItemType { SequenceNumeric = new NumericType { Value = 3m } };

        SpsTradeLineItemMapper.Map(source, Context).SequenceNumeric.Should().Be(3);
    }

    [Fact]
    public void Map_Description_FiltersToContextLanguage()
    {
        var source = new SPSTradeLineItemType
        {
            Description =
            [
                new TextType { languageID = "fr", Value = "Boeuf" },
                new TextType { languageID = "en", Value = "Beef" },
            ],
        };

        SpsTradeLineItemMapper.Map(source, Context).Description.Should().ContainSingle().Which.Should().Be("Beef");
    }

    [Fact]
    public void Map_Description_NoContextLanguageEntry_ReturnsNull()
    {
        var source = new SPSTradeLineItemType { Description = [new TextType { languageID = "fr", Value = "Boeuf" }] };

        SpsTradeLineItemMapper.Map(source, Context).Description.Should().BeNull();
    }

    [Fact]
    public void Map_ScientificName_FiltersToLatin()
    {
        var source = new SPSTradeLineItemType
        {
            ScientificName =
            [
                new TextType { languageID = "en", Value = "Donkey" },
                new TextType { languageID = "la", Value = "Equus asinus" },
            ],
        };

        SpsTradeLineItemMapper.Map(source, Context).ScientificName.Should().Be("Equus asinus");
    }

    [Fact]
    public void Map_ScientificName_NoLatinEntry_ReturnsNull()
    {
        var source = new SPSTradeLineItemType
        {
            ScientificName = [new TextType { languageID = "en", Value = "Donkey" }],
        };

        SpsTradeLineItemMapper.Map(source, Context).ScientificName.Should().BeNull();
    }

    [Fact]
    public void Map_NetAndGrossWeight_MapViaSpsMeasureMapper()
    {
        var source = new SPSTradeLineItemType
        {
            NetWeightMeasure = new MeasureType { Value = 100m, unitCode = "KGM" },
            GrossWeightMeasure = new MeasureType { Value = 110m, unitCode = "KGM" },
        };

        var result = SpsTradeLineItemMapper.Map(source, Context);

        result.NetWeight!.Content.Should().Be("100");
        result.GrossWeight!.Content.Should().Be("110");
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsTradeLineItemMapper.Map(new SPSTradeLineItemType(), Context);

        result.SequenceNumeric.Should().BeNull();
        result.Description.Should().BeNull();
        result.ScientificName.Should().BeNull();
        result.NetWeight.Should().BeNull();
        result.GrossWeight.Should().BeNull();
        result.ApplicableClassification.Should().BeNull();
        result.PhysicalReferencedLogisticsPackage.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() => SpsTradeLineItemMapper.MapList(null, Context).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() => SpsTradeLineItemMapper.MapList([], Context).Should().BeNull();
}
