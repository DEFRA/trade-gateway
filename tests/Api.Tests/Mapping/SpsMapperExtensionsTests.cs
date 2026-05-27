using System.Xml.Serialization;
using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsMapperExtensionsTests
{
    // XSD-generated enums use [XmlEnum(Name="...")] so the C# identifier (e.g. "Item1") differs
    // from the wire value (e.g. "1"). XmlEnumCode reads the attribute to return the wire value.

    [Fact]
    public void XmlEnumCode_EnumMemberWithXmlEnumAttribute_ReturnsAttributeName()
    {
        CargoTypeClassificationCodeContentType.Item1.XmlEnumCode().Should().Be("1");
    }

    [Fact]
    public void XmlEnumCode_EnumMemberWithMultiCharXmlEnumName_ReturnsFullAttributeName()
    {
        CargoTypeClassificationCodeContentType.Item10.XmlEnumCode().Should().Be("10");
    }

    [Fact]
    public void XmlEnumCode_EnumMemberWithoutXmlEnumAttribute_ReturnsMemberName()
    {
        PlainCodeType.SomeValue.XmlEnumCode().Should().Be("SomeValue");
    }

    [Fact]
    public void ForLanguage_ReturnsValueForMatchingLanguageId()
    {
        var source = new[]
        {
            new TextType { languageID = "fr", Value = "Boeuf" },
            new TextType { languageID = "en", Value = "Beef" }
        };

        source.ForLanguage("en").Should().Be("Beef");
    }

    [Fact]
    public void ForLanguage_FallsBackToNullLanguageId()
    {
        var source = new[]
        {
            new TextType { languageID = "fr", Value = "Boeuf" },
            new TextType { Value = "Fallback" }
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
    public void ForLanguage_NullSource_ReturnsNull() =>
        ((TextType[]?)null).ForLanguage("en").Should().BeNull();

    [Fact]
    public void ForLanguageList_ReturnsEntriesForMatchingLanguageId()
    {
        var source = new[]
        {
            new TextType { languageID = "fr", Value = "Boeuf" },
            new TextType { languageID = "en", Value = "Beef" },
            new TextType { languageID = "en", Value = "Beef (trimmed)" }
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
            new TextType { Value = "Fallback B" }
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
    public void ForLanguageList_NullSource_ReturnsNull() =>
        ((TextType[]?)null).ForLanguageList("en").Should().BeNull();

    private enum PlainCodeType { SomeValue }
}
