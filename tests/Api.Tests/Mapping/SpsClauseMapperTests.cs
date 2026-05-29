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
    public void Map_NullLanguageId_TakesPriorityOverContextLanguage()
    {
        var source = new SPSClauseType
        {
            Content =
            [
                new TextType { languageID = "en", Value = "English" },
                new TextType { Value = "Language-neutral" },
            ],
        };

        SpsClauseMapper.Map(source, Context)!.Content.Should().Be("Language-neutral");
    }

    [Fact]
    public void Map_NoNullLanguageId_UsesContextLanguage()
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
    public void Map_NoNullOrContextLanguage_FallsBackToAny()
    {
        var source = new SPSClauseType
        {
            Content = [new TextType { languageID = "de", Value = "Inhalt" }],
        };

        SpsClauseMapper.Map(source, Context)!.Content.Should().Be("Inhalt");
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
