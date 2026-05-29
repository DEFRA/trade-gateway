using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class TextTypeExtensionsTests
{
    [Fact]
    public void ForLanguage_ReturnsValueForMatchingLanguageId()
    {
        var source = new[]
        {
            new TextType { languageID = "fr", Value = "Boeuf" },
            new TextType { languageID = "en", Value = "Beef" },
        };

        source.ForLanguage("en").Should().Be("Beef");
    }

    [Fact]
    public void ForLanguage_FallsBackToNullLanguageId()
    {
        var source = new[]
        {
            new TextType { languageID = "fr", Value = "Boeuf" },
            new TextType { Value = "Fallback" },
        };

        source.ForLanguage("en").Should().Be("Fallback");
    }

    [Fact]
    public void ForLanguage_NoMatchAndNoNull_ReturnsNull()
    {
        var source = new[] { new TextType { languageID = "fr", Value = "Boeuf" } };

        source.ForLanguage("en").Should().BeNull();
    }

    [Fact]
    public void ForLanguage_NullSource_ReturnsNull() => ((TextType[]?)null).ForLanguage("en").Should().BeNull();

    [Fact]
    public void ForLanguageList_ReturnsEntriesForMatchingLanguageId()
    {
        var source = new[]
        {
            new TextType { languageID = "fr", Value = "Boeuf" },
            new TextType { languageID = "en", Value = "Beef" },
            new TextType { languageID = "en", Value = "Beef (trimmed)" },
        };

        source.ForLanguageList("en").Should().BeEquivalentTo("Beef", "Beef (trimmed)");
    }

    [Fact]
    public void ForLanguageList_FallsBackToNullLanguageIdEntries()
    {
        var source = new[]
        {
            new TextType { languageID = "fr", Value = "Boeuf" },
            new TextType { Value = "Fallback A" },
            new TextType { Value = "Fallback B" },
        };

        source.ForLanguageList("en").Should().BeEquivalentTo("Fallback A", "Fallback B");
    }

    [Fact]
    public void ForLanguageList_NoMatchAndNoNull_ReturnsNull()
    {
        var source = new[] { new TextType { languageID = "fr", Value = "Boeuf" } };

        source.ForLanguageList("en").Should().BeNull();
    }

    [Fact]
    public void ForLanguageList_NullSource_ReturnsNull() => ((TextType[]?)null).ForLanguageList("en").Should().BeNull();

    [Fact]
    public void ForNeutralOrLanguage_PrefersNullLanguageIdFirst()
    {
        var source = new[]
        {
            new TextType { languageID = "en", Value = "English" },
            new TextType { Value = "Neutral" },
        };

        source.ForNeutralOrLanguage("en").Should().Be("Neutral");
    }

    [Fact]
    public void ForNeutralOrLanguage_FallsBackToContextLanguage()
    {
        var source = new[] { new TextType { languageID = "en", Value = "English" } };

        source.ForNeutralOrLanguage("en").Should().Be("English");
    }

    [Fact]
    public void ForNeutralOrLanguage_FallsBackToFirstWhenNoNullOrContextLanguage()
    {
        var source = new[] { new TextType { languageID = "fr", Value = "Français" } };

        source.ForNeutralOrLanguage("en").Should().Be("Français");
    }

    [Fact]
    public void ForNeutralOrLanguage_NullSource_ReturnsNull() =>
        ((TextType[]?)null).ForNeutralOrLanguage("en").Should().BeNull();
}
