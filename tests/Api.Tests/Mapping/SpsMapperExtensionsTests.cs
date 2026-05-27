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

    private enum PlainCodeType
    {
        SomeValue,
    }
}
