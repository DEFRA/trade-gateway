using System.Text.Json;
using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsPartyMapperTests
{
    [Fact]
    public void Map_NullSource_ReturnsNull() => SpsPartyMapper.Map(null).Should().BeNull();

    [Fact]
    public void Map_BasicFields_MapCorrectly()
    {
        var source = new SPSPartyType
        {
            ID = new IDType { Value = "XI0000" },
            Name = new TextType { Value = "DAERA" },
        };

        var result = SpsPartyMapper.Map(source)!;

        result.Identifier.Should().Be("XI0000");
        result.Name.Should().Be("DAERA");
    }

    [Fact]
    public void Map_PersonName_MapsToDefinedContact()
    {
        var source = new SPSPartyType
        {
            SpecifiedSPSPerson = new SPSPersonType { Name = new TextType { Value = "Daniel Klemm" } },
        };

        var result = SpsPartyMapper.Map(source)!;

        result.DefinedContact.Should().ContainSingle().Which.PersonName.Should().Be("Daniel Klemm");
    }

    [Fact]
    public void Map_NoPerson_ReturnsNullDefinedContact()
    {
        var result = SpsPartyMapper.Map(new SPSPartyType())!;

        result.DefinedContact.Should().BeNull();
    }

    [Fact]
    public void Map_TypeCodes_MappedToCodedValues()
    {
        var source = new SPSPartyType
        {
            TypeCode =
            [
                new CodeType { Value = "AUTHORITY", name = "Authority", listID = "operator_activity_type" },
            ],
        };

        var result = SpsPartyMapper.Map(source)!;

        var typeCode = result.PartyTypeCode.Should().ContainSingle().Subject;
        typeCode.Value.Should().Be("AUTHORITY");
        typeCode.Name.Should().Be("Authority");
        typeCode.UrlId.Should().Be("https://traces-codelists.ec.europa.eu/operator_activity_type");
    }

    [Fact]
    public void Map_RoleCode_MappedToCodedValue()
    {
        var source = new SPSPartyType
        {
            RoleCode = new PartyRoleCodeType
            {
                Value = PartyRoleCodeContentType.VJ,
                name = "Authority",
                listID = "3035",
            },
        };

        var result = SpsPartyMapper.Map(source)!;

        result.PartyRoleCode!.Value.Should().Be("VJ");
        result.PartyRoleCode.Name.Should().Be("Authority");
        result.PartyRoleCode.UrlId.Should().Be("https://traces-codelists.ec.europa.eu/3035");
    }

    [Fact]
    public void Map_NullTypeCode_ReturnsNullPartyTypeCode()
    {
        var result = SpsPartyMapper.Map(new SPSPartyType())!;

        result.PartyTypeCode.Should().BeNull();
    }

    [Fact]
    public void Map_Address_DelegatesToSpsAddressMapper()
    {
        var source = new SPSPartyType
        {
            SpecifiedSPSAddress = new SPSAddressType { CountryID = new IDType { Value = "GB" } },
        };

        var result = SpsPartyMapper.Map(source)!;

        result.PostalAddress.Should().NotBeNull();
        result.PostalAddress!.CountryId.Should().Be("GB");
    }
}
