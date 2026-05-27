using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsClauseMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_NullSource_ReturnsNull() => SpsClauseMapper.Map(null, Context).Should().BeNull();

    [Fact]
    public void Map_PrefersContextLanguageContent()
    {
        var source = new SPSClauseType
        {
            ID = new IDType { Value = "PURPOSE" },
            Content =
            [
                new TextType { languageID = "fr", Value = "Contenu" },
                new TextType { languageID = "en", Value = "Content" },
            ],
        };

        var result = SpsClauseMapper.Map(source, Context)!;

        result.Identifier.Should().Be("PURPOSE");
        result.Content.Should().Be("Content");
    }

    [Fact]
    public void Map_NoContextLanguageContent_FallsBackToNullLanguageId()
    {
        var source = new SPSClauseType
        {
            Content = [new TextType { languageID = "de", Value = "Inhalt" }, new TextType { Value = "Fallback" }],
        };

        SpsClauseMapper.Map(source, Context)!.Content.Should().Be("Fallback");
    }

    [Fact]
    public void Map_NoMatchingLanguage_FallsBackToFirst()
    {
        var source = new SPSClauseType { Content = [new TextType { languageID = "de", Value = "Inhalt" }] };

        SpsClauseMapper.Map(source, Context)!.Content.Should().BeNull();
    }

    [Fact]
    public void Map_NullContent_ReturnsNullContent()
    {
        var source = new SPSClauseType { ID = new IDType { Value = "ID1" } };

        var result = SpsClauseMapper.Map(source, Context)!;

        result.Identifier.Should().Be("ID1");
        result.Content.Should().BeNull();
    }
}
