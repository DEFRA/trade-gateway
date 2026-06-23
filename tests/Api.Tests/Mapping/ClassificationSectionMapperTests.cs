using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class ClassificationSectionMapperTests
{
    [Fact]
    public void Map_ClassificationSectionType_MapsAllFields()
    {
        var source = new ClassificationSectionType
        {
            code = "0101",
            lms = true,
            active = false,
            Description = new TextType { Value = "Live horses" },
            ClassificationSectionChapter = new CodeType { Value = "01" },
            MetaCountryGroupScope =
            [
                new CodeType { Value = "GB" },
                new CodeType { Value = " " },
                new CodeType { Value = "XI" },
            ],
        };

        var result = ClassificationSectionMapper.Map(source);

        result.Should()
            .BeEquivalentTo(
                new
                {
                    ClassCode = "0101",
                    Chapter = "01",
                    Lms = true,
                    Description = "Live horses",
                    Active = false,
                    Scopes = new[] { "GB", "XI" },
                }
            );
    }

    [Fact]
    public void Map_ClassificationSectionReference_MapsAllFields()
    {
        var source = new ClassificationSectionReference
        {
            code = "DEL",
            chapter = "veterinary",
            lms = false,
            Description = new TextType { Value = "Dealers" },
            Scope =
            [
                new MetaCountryGroupReference { id = "EFTA" },
                new MetaCountryGroupReference { id = "EU" },
            ],
        };

        var result = ClassificationSectionMapper.Map(source);

        result.Should()
            .BeEquivalentTo(
                new
                {
                    ClassCode = "DEL",
                    Chapter = "veterinary",
                    Lms = false,
                    Description = "Dealers",
                    Scopes = new[] { "EFTA", "EU" },
                }
            );
    }
}
