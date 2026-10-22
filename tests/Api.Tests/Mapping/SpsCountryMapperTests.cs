using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsCountryMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_NullSource_ReturnsNull() => SpsCountryMapper.Map(null, Context).Should().BeNull();

    [Fact]
    public void Map_AllFields_MapCorrectly()
    {
        var source = new SPSCountryType
        {
            ID = new IDType { Value = "GB" },
            Name = [new TextType { Value = "United Kingdom" }],
        };

        var result = SpsCountryMapper.Map(source, Context)!;

        result.Code?.Value.Should().Be("GB");
        result.Code?.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public void Map_Name_PrefersContextLanguage()
    {
        var source = new SPSCountryType
        {
            Name =
            [
                new TextType { languageID = "fr", Value = "Royaume-Uni" },
                new TextType { languageID = "en", Value = "United Kingdom" },
            ],
        };

        SpsCountryMapper.Map(source, Context)!.Code?.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public void Map_Name_FallsBackToNullLanguageId()
    {
        var source = new SPSCountryType { Name = [new TextType { Value = "United Kingdom" }] };

        SpsCountryMapper.Map(source, Context)!.Code?.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public void Map_SubordinateSubDivision_MapsThrough()
    {
        var source = new SPSCountryType
        {
            ID = new IDType { Value = "NL" },
            SubordinateSPSCountrySubDivision =
            [
                new SPSCountrySubDivisionType
                {
                    FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item44 },
                    ActivityAuthorizedSPSParty =
                    [
                        new SPSPartyType
                        {
                            ID = new IDType { Value = "NL0002" },
                            RoleCode = new PartyRoleCodeType { Value = PartyRoleCodeContentType.RA },
                        },
                    ],
                },
            ],
        };

        var result = SpsCountryMapper.Map(source, Context)!;

        result.SubordinateTradeCountrySubDivision.Should().NotBeNull();
        result.SubordinateTradeCountrySubDivision!.Should().ContainSingle();
        result.SubordinateTradeCountrySubDivision[0].FunctionTypeCode.Content.Should().Be("44");
        result
            .SubordinateTradeCountrySubDivision[0]
            .ActivityAuthorizedParty.Should()
            .ContainSingle()
            .Which.PartyRoleCode!.Value.Should()
            .Be("RA");
    }

    [Fact]
    public void Map_MultipleSubdivisions_MapsAll()
    {
        var source = new SPSCountryType
        {
            SubordinateSPSCountrySubDivision =
            [
                new SPSCountrySubDivisionType
                {
                    FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item44 },
                },
                new SPSCountrySubDivisionType
                {
                    FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item42 },
                },
            ],
        };

        var result = SpsCountryMapper.Map(source, Context)!;

        result.SubordinateTradeCountrySubDivision!.Select(d => d.FunctionTypeCode.Content).Should().Equal("44", "42");
    }

    [Fact]
    public void Map_SubdivisionWithoutFunctionTypeCode_IsOmittedFromList()
    {
        var source = new SPSCountryType
        {
            SubordinateSPSCountrySubDivision =
            [
                new SPSCountrySubDivisionType { ID = new IDType { Value = "NL" } },
                new SPSCountrySubDivisionType
                {
                    FunctionTypeCode = new LocationFunctionCodeType { Value = LocationFunctionCodeContentType.Item44 },
                },
            ],
        };

        var result = SpsCountryMapper.Map(source, Context)!;

        result.SubordinateTradeCountrySubDivision.Should().ContainSingle();
        result.SubordinateTradeCountrySubDivision![0].FunctionTypeCode.Content.Should().Be("44");
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsCountryMapper.Map(new SPSCountryType(), Context)!;

        result.Code?.Value.Should().BeNull();
        result.Code?.Name.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() => SpsCountryMapper.MapList(null, Context).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() => SpsCountryMapper.MapList([], Context).Should().BeNull();

    [Fact]
    public void MapList_MultipleEntries_MapsAll()
    {
        var source = new[]
        {
            new SPSCountryType
            {
                ID = new IDType { Value = "GB" },
                Name = [new TextType { Value = "United Kingdom" }],
            },
            new SPSCountryType
            {
                ID = new IDType { Value = "FR" },
                Name = [new TextType { Value = "France" }],
            },
        };

        var result = SpsCountryMapper.MapList(source, Context)!;

        result.Should().HaveCount(2);
        result[0].Code?.Value.Should().Be("GB");
        result[1].Code?.Value.Should().Be("FR");
    }
}
