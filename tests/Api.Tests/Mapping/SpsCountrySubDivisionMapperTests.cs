using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsCountrySubDivisionMapperTests
{
    [Fact]
    public void Map_NullSource_ReturnsNull() => SpsCountrySubDivisionMapper.Map(null).Should().BeNull();

    [Fact]
    public void Map_MissingFunctionTypeCode_ReturnsNull()
    {
        var source = new SPSCountrySubDivisionType { ID = new IDType { Value = "NL" } };

        SpsCountrySubDivisionMapper.Map(source).Should().BeNull();
    }

    [Fact]
    public void Map_Authorities_MapRoleAndIdentity()
    {
        var source = new SPSCountrySubDivisionType
        {
            FunctionTypeCode = new LocationFunctionCodeType
            {
                Value = LocationFunctionCodeContentType.Item44,
                name = "Place of authentication of document",
            },
            ActivityAuthorizedSPSParty =
            [
                new SPSPartyType
                {
                    ID = new IDType { Value = "NL0002", schemeID = "authority_activity_id" },
                    Name = new TextType { Value = "NVWA" },
                    RoleCode = new PartyRoleCodeType { Value = PartyRoleCodeContentType.RA },
                    SpecifiedSPSAddress = new SPSAddressType
                    {
                        PostcodeCode = new CodeType { Value = "3511 LW" },
                        LineOne = new TextType { Value = "Catharijnesingel 59" },
                        CityName = new TextType { Value = "Utrecht" },
                        CountryID = new IDType { Value = "NL" },
                    },
                },
                new SPSPartyType
                {
                    ID = new IDType { Value = "NL0003" },
                    Name = new TextType { Value = "Local authority" },
                    RoleCode = new PartyRoleCodeType { Value = PartyRoleCodeContentType.VG },
                },
            ],
        };

        var result = SpsCountrySubDivisionMapper.Map(source)!;

        result.FunctionTypeCode.Content.Should().Be("44");
        result.ActivityAuthorizedParty.Should().HaveCount(2);
        result.ActivityAuthorizedParty!.Select(p => p.PartyRoleCode!.Value).Should().Equal("RA", "VG");
        result.ActivityAuthorizedParty![0].Identifier.Should().Be("NL0002");
        result.ActivityAuthorizedParty![0].Name.Should().Be("NVWA");
        result
            .ActivityAuthorizedParty![0]
            .PostalAddress.Should()
            .BeEquivalentTo(
                new Trade.Gateway.Api.Contract.Certificate.TradeAddress
                {
                    PostcodeCode = "3511 LW",
                    LineOne = "Catharijnesingel 59",
                    CityName = "Utrecht",
                    CountryId = "NL",
                }
            );
    }

    [Fact]
    public void Map_PartyWithUnrecognisedRoleCode_PassesThroughVerbatim()
    {
        var source = new SPSCountrySubDivisionType
        {
            FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item44 },
            ActivityAuthorizedSPSParty =
            [
                new SPSPartyType
                {
                    Name = new TextType { Value = "Border control post" },
                    RoleCode = new PartyRoleCodeType { Value = PartyRoleCodeContentType.CM },
                },
            ],
        };

        var result = SpsCountrySubDivisionMapper.Map(source)!;

        result.ActivityAuthorizedParty.Should().ContainSingle();
        result.ActivityAuthorizedParty![0].PartyRoleCode!.Value.Should().Be("CM");
    }

    [Fact]
    public void Map_Region_KeepsIdentifierAndUrlId()
    {
        var source = new SPSCountrySubDivisionType
        {
            ID = new IDType { Value = "IT-CAM", schemeID = "region_code" },
            FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item106 },
        };

        var result = SpsCountrySubDivisionMapper.Map(source)!;

        result.Identifier.Should().Be("IT-CAM");
        result.UrlId.Should().Be("https://traces-codelists.ec.europa.eu/region_code");
        result.FunctionTypeCode.Content.Should().Be("106");
        result.ActivityAuthorizedParty.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() => SpsCountrySubDivisionMapper.MapList(null).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() => SpsCountrySubDivisionMapper.MapList([]).Should().BeNull();

    [Fact]
    public void MapList_MultipleSubdivisions_MapsAll()
    {
        // INTRA.EU.XI.2026.0000009: place of authentication (44, central + local authority) followed by a
        // consignment exit customs office (42, border control post).
        var source = new[]
        {
            new SPSCountrySubDivisionType
            {
                FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item44 },
                ActivityAuthorizedSPSParty =
                [
                    new SPSPartyType
                    {
                        ID = new IDType { Value = "XI0000", schemeID = "authority_activity_id" },
                        Name = new TextType
                        {
                            Value = "Department of Agriculture, Environment and Rural Affairs (DAERA)",
                        },
                        RoleCode = new PartyRoleCodeType { Value = PartyRoleCodeContentType.RA },
                    },
                    new SPSPartyType
                    {
                        Name = new TextType
                        {
                            Value = "Department of Agriculture, Environment and Rural Affairs (DAERA)",
                        },
                        RoleCode = new PartyRoleCodeType { Value = PartyRoleCodeContentType.VG },
                    },
                ],
            },
            new SPSCountrySubDivisionType
            {
                FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item42 },
                ActivityAuthorizedSPSParty =
                [
                    new SPSPartyType
                    {
                        ID = new IDType { Value = "PL32010", schemeID = "authority_activity_id" },
                        Name = new TextType { Value = "Białogard" },
                        RoleCode = new PartyRoleCodeType { Value = PartyRoleCodeContentType.CM },
                    },
                ],
            },
        };

        var result = SpsCountrySubDivisionMapper.MapList(source)!;

        result.Should().HaveCount(2);
        result[0].FunctionTypeCode.Content.Should().Be("44");
        result[0].ActivityAuthorizedParty!.Select(p => p.PartyRoleCode!.Value).Should().Equal("RA", "VG");
        result[1].FunctionTypeCode.Content.Should().Be("42");
        result[1].ActivityAuthorizedParty.Should().ContainSingle().Which.PartyRoleCode!.Value.Should().Be("CM");
    }
}
