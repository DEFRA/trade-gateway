using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class ReferenceDataMapperTests
{
    [Fact]
    public void Map_ClassificationSections_MapsSoapResponse()
    {
        ClassificationSectionType[] response =
            [
                new ClassificationSectionType
                {
                    code = "0101",
                    lms = true,
                    Description = new TextType { Value = "Live horses" },
                    ClassificationSectionChapter = new CodeType { Value = "01" },
                    MetaCountryGroupScope =
                    [
                        new CodeType { Value = "GB" },
                        new CodeType { Value = "XI" },
                    ],
                },
            ];

        var result = ClassificationSectionMapper.Map(response);

        result.RetrievedAt.Should().NotBeNull();
        result.Sections.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    ClassCode = "0101",
                    Chapter = "01",
                    Lms = true,
                    Description = "Live horses",
                    Active = true,
                    Scopes = (string[])["GB", "XI"],
                }
            );
    }

    [Fact]
    public void Map_ClassificationTree_MapsSoapResponse()
    {
        TracesNT.WebServices.ClassificationTreeNode[] response =
            [
                new ClassificationTreeNode
                {
                    path = "intra_trade",
                    type = ClassificationTreeNodeType.taxon,
                    allowedForSelection = false,
                    Description = new TextType { Value = "EU Intra-trade" },
                    Node =
                    [
                        new ClassificationTreeNode
                        {
                            path = "intra_trade/0101",
                            type = ClassificationTreeNodeType.nomenclature,
                            allowedForSelection = true,
                            Description = new TextType { Value = "Live horses" },
                            Item = new CodeType { Value = "0101" },
                        },
                    ],
                },
            ];

        var result = ClassificationTreeMapper.Map(response, "intra_trade");

        result.TreeId.Should().Be("intra_trade");
        result.RetrievedAt.Should().NotBeNull();
        result.Nodes.Should().ContainSingle();
        result.Nodes![0].Path.Should().Be("intra_trade");
        result.Nodes[0].Label.Should().Be("EU Intra-trade");
        result.Nodes[0].NodeType.Should().Be("group");
        result.Nodes[0].Selectable.Should().BeFalse();
        result.Nodes[0].Children.Should().ContainSingle();
        result.Nodes[0].Children![0].CnCode.Should().Be("0101");
        result.Nodes[0].Children![0].NodeType.Should().Be("nomenclature");
    }

    [Fact]
    public void Map_ClassificationTreeNodeDetail_MapsSoapResponse()
    {
        var response = new GetClassificationTreeNodeDetailResponse(
            new MessageType(),
            new GetClassificationTreeNodeDetailResponseType
            {
                Node = new ClassificationTreeNodeDetail
                {
                    path = "intra_trade/0101",
                    allowedForSelection = true,
                    type = ClassificationTreeNodeType.nomenclature,
                    Description = new TextType { Value = "Live horses" },
                    Item = new CodeType { Value = "0101", listID = "CN" },
                    Attribute =
                    [
                        new BooleanNodeAttribute
                        {
                            id = "isActive",
                            Description = new TextType { Value = "Is active" },
                            BooleanValue = true,
                        },
                        new ClassificationSectionNodeAttribute
                        {
                            id = "sections",
                            Description = new TextType { Value = "Classification sections" },
                            ClassificationSection =
                            [
                                new ClassificationSectionReference
                                {
                                    code = "0101",
                                    chapter = "01",
                                    lms = true,
                                    Description = new TextType { Value = "Live horses" },
                                    Scope =
                                    [
                                        new MetaCountryGroupReference { Value = "GB" },
                                        new MetaCountryGroupReference { Value = "XI" },
                                    ],
                                },
                            ],
                        },
                        new TaxonNodeAttribute
                        {
                            id = "TAXON_POSSIBLE_VALUES",
                            Description = new TextType { Value = "Taxons" },
                            TaxonReference =
                            [
                                new TaxonReference
                                {
                                    taxonId = 123,
                                    eppoCode = "EQCAB",
                                    faoCode = "HOR",
                                    Value = "Equus caballus",
                                },
                            ],
                        },
                        new TaxonNodeAttribute
                        {
                            id = "INVASIVE_TAXON_POSSIBLE_VALUES",
                            Description = new TextType { Value = "Invasive taxons" },
                            TaxonReference =
                            [
                                new TaxonReference
                                {
                                    taxonId = 456,
                                    eppoCode = "ABCDEF",
                                    faoCode = "INV",
                                    Value = "Invasivus exampleus",
                                },
                            ],
                        },
                        new SelectableDocumentLinkNodeAttribute
                        {
                            id = "SELECTABLE_DOCUMENT_LINKS",
                            Description = new TextType { Value = "Selectable document links" },
                            DocumentTypeValue =
                            [
                                new SelectableDocumentLinkNodeAttributeValue { Value = "EU_INTRA", linkType = "ATTACHED_TO" },
                                new SelectableDocumentLinkNodeAttributeValue { Value = "ACCOMPANYING_DOCUMENT", linkType = "ATTACHED_TO" },
                                new SelectableDocumentLinkNodeAttributeValue { Value = "JOURNEY_LOG", linkType = "ATTACHED_TO" },
                                new SelectableDocumentLinkNodeAttributeValue { Value = "EU_EXPORT", linkType = "ATTACHED_TO" },
                            ],
                        },
                    ],
                },
            }
        );

        var result = ClassificationTreeNodeDetailMapper.Map(response, "intra_trade");

        result.Source.Should().Be("traces");
        result.TreeId.Should().Be("intra_trade");
        result.NodePath.Should().Be("intra_trade/0101");
        result.Node.Should()
            .BeEquivalentTo(
                new
                {
                    CnCode = "0101",
                    CertificateModel = (object?)null,
                    Selectable = true,
                    NodeType = "nomenclature",
                }
            );
        result.Attributes.Should().ContainSingle(attribute => attribute.Key == "isActive");
        result.ClassificationSectionGroups.Should().ContainSingle();
        result.Taxons.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    TaxonId = 123,
                    EppoCode = "EQCAB",
                    FaoCode = "HOR",
                    Name = "Equus caballus",
                    LanguageId = (string?)null,
                }
            );

        result.InvasiveTaxons.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    TaxonId = 456,
                    EppoCode = "ABCDEF",
                    FaoCode = "INV",
                    Name = "Invasivus exampleus",
                    LanguageId = (string?)null,
                }
            );

        result.DocumentTypes.Should().ContainSingle();
        result.DocumentTypes![0].Should()
            .BeEquivalentTo(
                new
                {
                    Key = "SELECTABLE_DOCUMENT_LINKS",
                    Description = "Selectable document links",
                    DocumentLinkTypes = new[]
                    {
                        new { DocumentType = "EU_INTRA", LinkType = "ATTACHED_TO" },
                        new { DocumentType = "ACCOMPANYING_DOCUMENT", LinkType = "ATTACHED_TO" },
                        new { DocumentType = "JOURNEY_LOG", LinkType = "ATTACHED_TO" },
                        new { DocumentType = "EU_EXPORT", LinkType = "ATTACHED_TO" },
                    }
                }
            );
    }

    [Fact]
    public void Map_ClassificationTreeNodeDetail_CertificateModelNode_MapsModelIdOnly()
    {
        var response = new GetClassificationTreeNodeDetailResponse(
            new MessageType(),
            new GetClassificationTreeNodeDetailResponseType
            {
                Node = new ClassificationTreeNodeDetail
                {
                    path = "R/N-10000/C-11978",
                    allowedForSelection = true,
                    type = ClassificationTreeNodeType.certificate_model,
                    Description = new TextType { Value = "Model Description" },
                    Item = new CertificateModelReference
                    {
                        modelId = 11978,
                        ShortTitle = new TextType { Value = "11978" },
                        LongTitle = new TextType { Value = "Model Description" },
                        createdOn = new DateTime(2022, 12, 7, 19, 3, 10, DateTimeKind.Utc),
                        updatedOn = new DateTime(2022, 12, 7, 19, 3, 10, DateTimeKind.Utc),
                        updatedOnSpecified = true,
                    },
                },
            }
        );

        var result = ClassificationTreeNodeDetailMapper.Map(response, "intra_trade");

        result.Node.Should().NotBeNull();
        result.Node!.CnCode.Should().BeNull();
        result.Node.NodeType.Should().Be("certificate");
        result.Node.Label.Should().Be("Model Description");
    }
}
