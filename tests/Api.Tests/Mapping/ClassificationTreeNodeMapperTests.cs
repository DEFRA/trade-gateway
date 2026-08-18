using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class ClassificationTreeNodeMapperTests
{
    [Fact]
    public void Map_MapsNodeRecursively()
    {
        var source = new ClassificationTreeNode
        {
            path = "root",
            type = ClassificationTreeNodeType.taxon,
            allowedForSelection = false,
            Description = new TextType { Value = "Root" },
            Node =
            [
                new ClassificationTreeNode
                {
                    path = "root/0101",
                    type = ClassificationTreeNodeType.nomenclature,
                    allowedForSelection = true,
                    Description = new TextType { Value = "Child" },
                    Item = new CodeType { Value = "0101" },
                },
            ],
        };

        var result = ClassificationTreeMapper.Map(source)!;

        result.Path.Should().Be("root");
        result.Label.Should().Be("Root");
        result.NodeType.Should().Be("group");
        result.Selectable.Should().BeFalse();
        result.CnCode.Should().BeNull();
        result.Children.Should().ContainSingle();
        result
            .Children![0]
            .Should()
            .BeEquivalentTo(
                new
                {
                    Path = "root/0101",
                    Label = "Child",
                    NodeType = "nomenclature",
                    Selectable = true,
                    CnCode = "0101",
                }
            );
    }
}
