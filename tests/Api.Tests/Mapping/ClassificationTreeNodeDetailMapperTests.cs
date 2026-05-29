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
                }
            );
    }

    [Fact]
    public void Map_CertificateModelNode_MapsModelId()
    {
        var source = new ClassificationTreeNodeDetail
        {
            Item = new CertificateModelReference { modelId = 11978 },
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
                    NodeType = "other",
                }
            );
    }

    [Fact]
    public void MapResolvedProductClassification_CodeNode_MapsClassification()
    {
        var source = new ClassificationTreeNodeDetail
        {
            Item = new CodeType { Value = "0101", listID = "CN" },
            Description = new TextType { Value = "Live horses" },
        };

        var result = ClassificationTreeNodeDetailMapper.MapResolvedProductClassification(source)!;

        result.Should()
            .BeEquivalentTo(
                new
                {
                    SystemId = "CN",
                    ClassCode = "0101",
                    ClassName = new[] { "Live horses" },
                }
            );
    }

    [Fact]
    public void MapResolvedProductClassification_NonCodeNode_ReturnsNull()
    {
        var source = new ClassificationTreeNodeDetail
        {
            Item = new CertificateModelReference { modelId = 11978 },
            Description = new TextType { Value = "Model" },
        };

        ClassificationTreeNodeDetailMapper.MapResolvedProductClassification(source).Should().BeNull();
    }
}
