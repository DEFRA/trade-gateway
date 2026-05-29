using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsConsignmentMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_AllPartySlots_MapFromCorrectSourceFields()
    {
        var source = new SPSConsignmentType
        {
            ConsignorSPSParty = new SPSPartyType { ID = new IDType { Value = "CONSIGNOR" } },
            ConsigneeSPSParty = new SPSPartyType { ID = new IDType { Value = "CONSIGNEE" } },
            DespatchSPSParty = new SPSPartyType { ID = new IDType { Value = "DESPATCH" } },
            CustomsTransitAgentSPSParty = new SPSPartyType { ID = new IDType { Value = "CUSTOMS" } },
        };

        var result = SpsConsignmentMapper.Map(source, Context);

        result.ConsignorParty!.Identifier.Should().Be("CONSIGNOR");
        result.ConsigneeParty!.Identifier.Should().Be("CONSIGNEE");
        result.DespatchParty!.Identifier.Should().Be("DESPATCH");
        result.CustomsTransitAgentParty!.Identifier.Should().Be("CUSTOMS");
    }

    [Fact]
    public void Map_NullParties_ReturnNullTargetFields()
    {
        var result = SpsConsignmentMapper.Map(new SPSConsignmentType(), Context);

        result.ConsignorParty.Should().BeNull();
        result.ConsigneeParty.Should().BeNull();
        result.DespatchParty.Should().BeNull();
        result.CustomsTransitAgentParty.Should().BeNull();
    }

    [Fact]
    public void Map_UnloadingBaseportLocationAndConsignmentItems_AreNull()
    {
        var result = SpsConsignmentMapper.Map(new SPSConsignmentType(), Context);

        result.UnloadingBaseportLocation.Should().BeNull();
        result.IncludedConsignmentItem.Should().BeNull();
    }

    [Fact]
    public void Map_SingleCountries_MapFromCorrectSourceFields()
    {
        var source = new SPSConsignmentType
        {
            ExportSPSCountry = new SPSCountryType { ID = new IDType { Value = "GB" } },
            ImportSPSCountry = new SPSCountryType { ID = new IDType { Value = "FR" } },
        };

        var result = SpsConsignmentMapper.Map(source, Context);

        result.ExportCountry!.Id.Should().Be("GB");
        result.ImportCountry!.Id.Should().Be("FR");
    }

    [Fact]
    public void Map_ArrayCountries_MapFromCorrectSourceFields()
    {
        var source = new SPSConsignmentType
        {
            ReExportSPSCountry = [new SPSCountryType { ID = new IDType { Value = "DE" } }],
            TransitSPSCountry =
            [
                new SPSCountryType { ID = new IDType { Value = "BE" } },
                new SPSCountryType { ID = new IDType { Value = "NL" } },
            ],
        };

        var result = SpsConsignmentMapper.Map(source, Context);

        result.ReExportCountry.Should().ContainSingle().Which.Id.Should().Be("DE");
        result.TransitCountry!.Select(c => c.Id).Should().BeEquivalentTo("BE", "NL");
    }

    [Fact]
    public void Map_NullCountries_ReturnNullTargetFields()
    {
        var result = SpsConsignmentMapper.Map(new SPSConsignmentType(), Context);

        result.ExportCountry.Should().BeNull();
        result.ImportCountry.Should().BeNull();
        result.ReExportCountry.Should().BeNull();
        result.TransitCountry.Should().BeNull();
    }
}
