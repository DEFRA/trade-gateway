using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsClassificationMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_AllFields_MapCorrectly()
    {
        var source = new SPSClassificationType
        {
            SystemID = new IDType { Value = "CN" },
            SystemName = [new TextType { Value = "Combined Nomenclature" }],
            ClassCode = new CodeType { Value = "0201" },
            ClassName = [new TextType { Value = "Beef" }, new TextType { Value = "Boeuf" }]
        };

        var result = SpsClassificationMapper.Map(source, Context);

        result.SystemId.Should().Be("CN");
        result.SystemName.Should().Be("Combined Nomenclature");
        result.ClassCode.Should().Be("0201");
        result.ClassName.Should().BeEquivalentTo("Beef", "Boeuf");
    }

    [Fact]
    public void Map_SystemName_PrefersContextLanguage()
    {
        var source = new SPSClassificationType
        {
            SystemName =
            [
                new TextType { languageID = "fr", Value = "Nomenclature combinée" },
                new TextType { languageID = "en", Value = "Combined Nomenclature" }
            ]
        };

        SpsClassificationMapper.Map(source, Context).SystemName.Should().Be("Combined Nomenclature");
    }

    [Fact]
    public void Map_ClassName_PrefersContextLanguage()
    {
        var source = new SPSClassificationType
        {
            ClassName =
            [
                new TextType { languageID = "fr", Value = "Boeuf" },
                new TextType { languageID = "en", Value = "Beef" }
            ]
        };

        SpsClassificationMapper.Map(source, Context).ClassName.Should().ContainSingle().Which.Should().Be("Beef");
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsClassificationMapper.Map(new SPSClassificationType(), Context);

        result.SystemId.Should().BeNull();
        result.SystemName.Should().BeNull();
        result.ClassCode.Should().BeNull();
        result.ClassName.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() =>
        SpsClassificationMapper.MapList(null, Context).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() =>
        SpsClassificationMapper.MapList([], Context).Should().BeNull();
}
