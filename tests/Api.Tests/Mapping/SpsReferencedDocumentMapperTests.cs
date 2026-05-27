using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsReferencedDocumentMapperTests
{
    [Fact]
    public void Map_ExtractsXmlEnumCodeForTypeCode()
    {
        var source = new SPSReferencedDocumentType
        {
            TypeCode = new DocumentCodeType { Value = DocumentNameCodeContentType.Item856 },
            ID = new IDType { Value = "REF-001" }
        };

        var result = SpsReferencedDocumentMapper.Map(source);

        result.TypeCode.Should().Be("856");
        result.Identifier.Should().Be("REF-001");
    }

    [Fact]
    public void Map_SingleInformation_WrapsInList()
    {
        var source = new SPSReferencedDocumentType
        {
            Information = new TextType { Value = "Some info" }
        };

        var result = SpsReferencedDocumentMapper.Map(source);

        result.Information.Should().ContainSingle().Which.Should().Be("Some info");
    }

    [Fact]
    public void Map_NullInformation_ReturnsNullList()
    {
        var result = SpsReferencedDocumentMapper.Map(new SPSReferencedDocumentType());

        result.Information.Should().BeNull();
    }

    [Fact]
    public void Map_AttachmentBinaryObject_IsNull()
    {
        var source = new SPSReferencedDocumentType
        {
            AttachmentBinaryObject = [new BinaryObjectType { Value = [1, 2, 3] }]
        };

        SpsReferencedDocumentMapper.Map(source).AttachmentBinaryObject.Should().BeNull();
    }
}
