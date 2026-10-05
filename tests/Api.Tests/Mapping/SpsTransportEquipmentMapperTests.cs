using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsTransportEquipmentMapperTests
{
    [Fact]
    public void MapList_NullSource_ReturnsNull() => SpsTransportEquipmentMapper.MapList(null).Should().BeNull();

    [Fact]
    public void MapList_EquipmentWithSeals_MapsIdentifiersAndSchemeUris()
    {
        var source = new SPSTransportEquipmentType
        {
            ID = new IDType { Value = "MSKU1234567", schemeID = "container_number" },
            AffixedSPSSeal = [new SPSSealType { ID = new IDType { Value = "SEAL001", schemeID = "seal_number" } }],
        };

        var result = SpsTransportEquipmentMapper.MapList([source])!.Single();

        result.Identifier.Should().Be("MSKU1234567");
        result.UrlId.Should().Be("https://traces-codelists.ec.europa.eu/container_number");
        result.AffixedLogisticsSeal.Should().ContainSingle().Which.Identifier.Should().Be("SEAL001");
        result
            .AffixedLogisticsSeal![0].UrlId.Should()
            .Be("https://traces-codelists.ec.europa.eu/seal_number");
    }

    [Fact]
    public void MapList_EquipmentWithoutSeals_LeavesSealsNull()
    {
        var result = SpsTransportEquipmentMapper.MapList([new SPSTransportEquipmentType()])!.Single();

        result.Identifier.Should().BeNull();
        result.AffixedLogisticsSeal.Should().BeNull();
    }
}
