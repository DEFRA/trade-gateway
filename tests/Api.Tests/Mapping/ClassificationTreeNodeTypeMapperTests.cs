using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class ClassificationTreeNodeTypeMapperTests
{
    [Theory]
    [InlineData(ClassificationTreeNodeType.nomenclature, "nomenclature")]
    [InlineData(ClassificationTreeNodeType.label, "label")]
    [InlineData(ClassificationTreeNodeType.taxon, "group")]
    [InlineData(ClassificationTreeNodeType.certificate_model, "certificate")]
    [InlineData(ClassificationTreeNodeType.no_commodity, "other")]
    public void Map_ReturnsExpectedNodeType(ClassificationTreeNodeType source, string expected)
    {
        ClassificationTreeNodeTypeMapper.Map(source).Should().Be(expected);
    }
}
