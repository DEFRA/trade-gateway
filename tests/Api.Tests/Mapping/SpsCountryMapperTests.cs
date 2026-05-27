using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsCountryMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_NullSource_ReturnsNull() =>
        SpsCountryMapper.Map(null, Context).Should().BeNull();

    [Fact]
    public void Map_AllFields_MapCorrectly()
    {
        var source = new SPSCountryType
        {
            ID = new IDType { Value = "GB" },
            Name = [new TextType { Value = "United Kingdom" }]
        };

        var result = SpsCountryMapper.Map(source, Context)!;

        result.Id.Should().Be("GB");
        result.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public void Map_Name_PrefersContextLanguage()
    {
        var source = new SPSCountryType
        {
            Name =
            [
                new TextType { languageID = "fr", Value = "Royaume-Uni" },
                new TextType { languageID = "en", Value = "United Kingdom" }
            ]
        };

        SpsCountryMapper.Map(source, Context)!.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public void Map_Name_FallsBackToNullLanguageId()
    {
        var source = new SPSCountryType
        {
            Name = [new TextType { Value = "United Kingdom" }]
        };

        SpsCountryMapper.Map(source, Context)!.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsCountryMapper.Map(new SPSCountryType(), Context)!;

        result.Id.Should().BeNull();
        result.Name.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() =>
        SpsCountryMapper.MapList(null, Context).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() =>
        SpsCountryMapper.MapList([], Context).Should().BeNull();

    [Fact]
    public void MapList_MultipleEntries_MapsAll()
    {
        var source = new[]
        {
            new SPSCountryType { ID = new IDType { Value = "GB" }, Name = [new TextType { Value = "United Kingdom" }] },
            new SPSCountryType { ID = new IDType { Value = "FR" }, Name = [new TextType { Value = "France" }] }
        };

        var result = SpsCountryMapper.MapList(source, Context)!;

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("GB");
        result[1].Id.Should().Be("FR");
    }
}
