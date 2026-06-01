using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class ClassificationTreeNodeDetailMapperTests
{
    [Fact]
    public void Map_NullSource_ReturnsNull()
    {
        ClassificationTreeNodeDetailMapper.Map(null).Should().BeNull();
    }

    [Fact]
    public void Map_CodeNode_MapsCnCode()
    {
        var source = new ClassificationTreeNodeDetail
        {
            Item = new CodeType { Value = "0101" },
            Description = new TextType { Value = "Live horses" },
            type = ClassificationTreeNodeType.nomenclature,
            allowedForSelection = true,
        };

        var result = ClassificationTreeNodeDetailMapper.Map(source)!;

        result.Should()
            .BeEquivalentTo(
                new
                {
                    CnCode = "0101",
                    ModelId = (string?)null,
                    Selectable = true,
                    NodeType = "nomenclature",
                    Label = "Live horses",
                }
            );
    }

    [Fact]
    public void Map_CertificateModelNode_MapsModelId()
    {
        var source = new ClassificationTreeNodeDetail
        {
            Item = new CertificateModelReference { modelId = 11978 },
            Description = new TextType { Value = "Model title" },
            type = ClassificationTreeNodeType.certificate_model,
            allowedForSelection = false,
        };

        var result = ClassificationTreeNodeDetailMapper.Map(source)!;

        result.Should()
            .BeEquivalentTo(
                new
                {
                    CnCode = (string?)null,
                    ModelId = "11978",
                    Selectable = false,
                    NodeType = "certificate",
                    Label = "Model title",
                }
            );
    }

    [Fact]
    public void Map_DetailResponse_MapsLegislationAttributes()
    {
        var source = new ClassificationTreeNodeDetail
        {
            path = "R/N-10000/N-10065",
            Description = new TextType { Value = "Live horses" },
            allowedForSelection = true,
            type = ClassificationTreeNodeType.nomenclature,
            Attribute =
            [
                new LegislationNodeAttribute
                {
                    id = "LEGISLATION",
                    Description = new TextType { Value = "Applicable legislation" },
                    LegislationReference = new LegislationReference
                    {
                        legislationId = 123,
                        CelexIdentifier = [new IDType { Value = "32020R0692" }],
                        CertificateModel =
                        [
                            new CertificateModelReference
                            {
                                modelId = 11822,
                                ShortTitle = new TextType { Value = "11822" },
                                LongTitle = new TextType { Value = "Equine model" },
                                createdOn = new DateTime(2024, 3, 7, 16, 12, 23, DateTimeKind.Utc),
                                updatedOn = new DateTime(2024, 3, 8, 16, 12, 23, DateTimeKind.Utc),
                                updatedOnSpecified = true,
                            },
                        ],
                        OriginCountry = [new IDType { Value = "GB" }],
                        DestinationCountry = [new IDType { Value = "FR" }],
                        OriginClassificationSection =
                        [
                            new ClassificationSectionReference
                            {
                                code = "ORIG",
                                chapter = "01",
                                lms = false,
                                Description = new TextType { Value = "Origin section" },
                                Scope = [new MetaCountryGroupReference { Value = "EU" }],
                            },
                        ],
                        DestinationClassificationSection =
                        [
                            new ClassificationSectionReference
                            {
                                code = "DEST",
                                chapter = "02",
                                lms = true,
                                Description = new TextType { Value = "Destination section" },
                                Scope = [new MetaCountryGroupReference { Value = "EU" }],
                            },
                        ],
                    },
                },
            ],
        };

        var result = ClassificationTreeNodeDetailMapper.Map(source, "cheda");

        result.Attributes.Should().BeNull();
        result.LegislationAttributes.Should().ContainSingle();
        result.LegislationAttributes![0].Should()
            .BeEquivalentTo(
                new
                {
                    Key = "LEGISLATION",
                    Description = "Applicable legislation",
                    Legislation = new[]
                    {
                        new
                        {
                            LegislationId = 123,
                            CelexIdentifiers = new[] { "32020R0692" },
                            OriginCountries = new[] { "GB" },
                            DestinationCountries = new[] { "FR" },
                            CertificateModels = new[]
                            {
                                new
                                {
                                    ModelId = 11822,
                                    ShortTitle = "11822",
                                    LongTitle = "Equine model",
                                },
                            },
                            OriginClassificationSections = new[]
                            {
                                new
                                {
                                    ClassCode = "ORIG",
                                    Chapter = "01",
                                    Lms = false,
                                    Description = "Origin section",
                                    Scopes = new[] { "EU" },
                                },
                            },
                            DestinationClassificationSections = new[]
                            {
                                new
                                {
                                    ClassCode = "DEST",
                                    Chapter = "02",
                                    Lms = true,
                                    Description = "Destination section",
                                    Scopes = new[] { "EU" },
                                },
                            },
                        },
                    },
                }
            );
    }
}
