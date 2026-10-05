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
    public void Map_NoUnloadingLocationOrConsignmentItems_AreNull()
    {
        var result = SpsConsignmentMapper.Map(new SPSConsignmentType(), Context);

        result.UnloadingBaseportLocation.Should().BeNull();
        result.IncludedConsignmentItem.Should().BeNull();
    }

    [Fact]
    public void Map_UnloadingBaseportLocation_MapsFromCorrectSourceFields()
    {
        var source = new SPSConsignmentType
        {
            UnloadingBaseportSPSLocation = new SPSLocationType
            {
                ID = new IDType { Value = "GBDVR1", schemeID = "un_locode" },
                Name = [new TextType { Value = "Dover", languageID = "en" }],
            },
        };

        var result = SpsConsignmentMapper.Map(source, Context);

        result.UnloadingBaseportLocation!.Identifier.Should().Be("GBDVR1");
        result.UnloadingBaseportLocation.UrlId.Should().Be("https://traces-codelists.ec.europa.eu/un_locode");
        result.UnloadingBaseportLocation.Name.Should().Be("Dover");
    }

    [Fact]
    public void Map_LoadingBaseportLocation_MapsFromCorrectSourceFields()
    {
        var source = new SPSConsignmentType
        {
            LoadingBaseportSPSLocation = new SPSLocationType
            {
                ID = new IDType { Value = "GBLON1", schemeID = "un_locode" },
            },
        };

        var result = SpsConsignmentMapper.Map(source, Context);

        result.LoadingBaseportLocation!.Identifier.Should().Be("GBLON1");
    }

    [Fact]
    public void Map_PresentExaminationEvent_MapsToSingleEvent()
    {
        var source = new SPSConsignmentType
        {
            ExaminationSPSEvent = new SPSEventType
            {
                OccurrenceSPSLocation = new SPSLocationType { ID = new IDType { Value = "GBDVR1" } },
            },
        };

        var result = SpsConsignmentMapper.Map(source, Context);

        result.ExaminationEvent.Should().ContainSingle();
        result.ExaminationEvent![0].OccurrenceLogisticsLocation!.Identifier.Should().Be("GBDVR1");
    }

    [Fact]
    public void Map_UtilizedTransportEquipment_MapsWithSeals()
    {
        var source = new SPSConsignmentType
        {
            UtilizedSPSTransportEquipment =
            [
                new SPSTransportEquipmentType
                {
                    ID = new IDType { Value = "MSKU1234567" },
                    AffixedSPSSeal = [new SPSSealType { ID = new IDType { Value = "SEAL001" } }],
                },
            ],
        };

        var result = SpsConsignmentMapper.Map(source, Context);

        var equipment = result.UtilizedLogisticsTransportEquipment.Should().ContainSingle().Subject;
        equipment.Identifier.Should().Be("MSKU1234567");
        equipment.AffixedLogisticsSeal.Should().ContainSingle().Which.Identifier.Should().Be("SEAL001");
    }

    [Fact]
    public void Map_NoLoadingExaminationOrEquipment_AreNull()
    {
        var result = SpsConsignmentMapper.Map(new SPSConsignmentType(), Context);

        result.LoadingBaseportLocation.Should().BeNull();
        result.ExaminationEvent.Should().BeNull();
        result.UtilizedLogisticsTransportEquipment.Should().BeNull();
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

        result.ExportCountry!.Code?.Value.Should().Be("GB");
        result.ImportCountry!.Code?.Value.Should().Be("FR");
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

        result.ReExportCountry.Should().ContainSingle().Which.Code?.Value.Should().Be("DE");
        result.TransitCountry!.Select(c => c.Code?.Value).Should().BeEquivalentTo("BE", "NL");
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
