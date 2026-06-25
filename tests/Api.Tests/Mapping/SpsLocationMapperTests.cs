using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsLocationMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_Null_ReturnsNull() => SpsLocationMapper.Map(null, Context).Should().BeNull();

    [Fact]
    public void Map_AllFields_MapFromCorrectSourceFields()
    {
        var source = new SPSLocationType
        {
            ID = new IDType { Value = "GBDVR1", schemeID = "un_locode" },
            Name = [new TextType { Value = "Dover", languageID = "en" }],
        };

        var result = SpsLocationMapper.Map(source, Context)!;

        result.Identifier.Should().Be("GBDVR1");
        result.UrlId.Should().Be("https://traces-codelists.ec.europa.eu/un_locode");
        result.Name.Should().Be("Dover");
    }

    [Fact]
    public void Map_NoSchemeId_ReturnsNullUrlId()
    {
        var source = new SPSLocationType { ID = new IDType { Value = "GBDVR1" } };

        var result = SpsLocationMapper.Map(source, Context)!;

        result.Identifier.Should().Be("GBDVR1");
        result.UrlId.Should().BeNull();
    }

    [Fact]
    public void Map_EmptyLocation_ReturnsNullTargetFields()
    {
        var result = SpsLocationMapper.Map(new SPSLocationType(), Context)!;

        result.Should().NotBeNull();
        result.Identifier.Should().BeNull();
        result.UrlId.Should().BeNull();
        result.Name.Should().BeNull();
    }
}
