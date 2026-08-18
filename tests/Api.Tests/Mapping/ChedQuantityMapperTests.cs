using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Tests.Mapping;

public class ChedQuantityMapperTests
{
    private const string Mrn = "26GB16RF3TDPZE7AR2";

    [Fact]
    public void MapLedger_MapsAvailableQuantities()
    {
        var summary = new QuantityManagementCommoditySummaryEnhanced4ChedR51Type
        {
            AvailableQuantity =
            [
                new ProductQuantityEnhancedPlusPlusType
                {
                    CommodityCode = new CommodityCodeEnhanced4AvailableType
                    {
                        HarmonizedSystemSubheadingcode = "020329",
                        CombinedNomenclatureCode = "02032955",
                        TARICCode = "0203295500",
                    },
                    SwSupportingDocument = new SWSupportingDocumentType
                    {
                        UnitOfMeasure = UniversalUnitOfMeasureType.KGM,
                        Quantity = 1250.5m,
                        CertificateLineNumber = "1",
                    },
                },
            ],
        };

        var ledger = ChedQuantityMapper.MapLedger(summary);

        var available = ledger.Available.Should().ContainSingle().Subject;
        available.Quantity.Should().Be(1250.5m);
        available.UnitOfMeasure.Should().Be("KGM");
        available.CertificateLineNumber.Should().Be(1);
        available.CommodityCode!.HarmonizedSystemSubheadingCode.Should().Be("020329");
        available.CommodityCode.CombinedNomenclatureCode.Should().Be("02032955");
        available.CommodityCode.TaricCode.Should().Be("0203295500");
    }

    [Fact]
    public void MapLedger_WhenNoCommodityCodeParts_OmitsTheCommodityCodeEntirely()
    {
        var summary = new QuantityManagementCommoditySummaryEnhanced4ChedR51Type
        {
            AvailableQuantity =
            [
                new ProductQuantityEnhancedPlusPlusType
                {
                    CommodityCode = new CommodityCodeEnhanced4AvailableType(),
                    SwSupportingDocument = new SWSupportingDocumentType { Quantity = 1m },
                },
            ],
        };

        var ledger = ChedQuantityMapper.MapLedger(summary);

        ledger.Available.Single().CommodityCode.Should().BeNull();
    }

    /// <summary>
    /// Documents R3. <c>UniversalUnitOfMeasureType</c> has no <c>Specified</c> companion and its
    /// first member is <c>TNE</c>, so an omitted <c>UnitOfMeasure</c> element is indistinguishable
    /// from an explicit "tonnes" once deserialised. Nothing in the mapper can recover this; the
    /// guard is that TracesNT always sends the element. If this test ever starts failing because
    /// the generated type gained a <c>Specified</c> companion, map it to null instead.
    /// </summary>
    [Fact]
    public void MapLedger_AnAbsentUnitOfMeasureIsIndistinguishableFromTonnes()
    {
        var summary = new QuantityManagementCommoditySummaryEnhanced4ChedR51Type
        {
            AvailableQuantity =
            [
                new ProductQuantityEnhancedPlusPlusType
                {
                    // Nothing assigned to UnitOfMeasure — exactly the state an omitted element leaves.
                    SwSupportingDocument = new SWSupportingDocumentType { Quantity = 10m },
                },
            ],
        };

        var ledger = ChedQuantityMapper.MapLedger(summary);

        ledger.Available.Single().UnitOfMeasure.Should().Be("TNE");
    }

    [Fact]
    public void MapLedger_WhenSupportingDocumentIsAbsent_UnitOfMeasureIsNull()
    {
        var summary = new QuantityManagementCommoditySummaryEnhanced4ChedR51Type
        {
            AvailableQuantity = [new ProductQuantityEnhancedPlusPlusType()],
        };

        var ledger = ChedQuantityMapper.MapLedger(summary);

        var available = ledger.Available.Single();
        available.UnitOfMeasure.Should().BeNull();
        available.Quantity.Should().Be(0m);
    }

    /// <summary>
    /// An entirely empty summary maps to empty arrays, not to nulls. Note that this is also what an
    /// upstream that stopped reporting allocations altogether would produce — the two are
    /// indistinguishable, which is why the QMI=0 question in ADR-0006 has to be settled by
    /// observation rather than by anything in this mapper.
    /// </summary>
    [Fact]
    public void MapLedger_WhenSummaryIsEmpty_EverythingIsAnEmptyArray()
    {
        var ledger = ChedQuantityMapper.MapLedger(new QuantityManagementCommoditySummaryEnhanced4ChedR51Type());

        ledger.Available.Should().BeEmpty();
        ledger.Allocations!.Reserved.Should().BeEmpty();
        ledger.Allocations.Consumed.Should().BeEmpty();
    }

    [Fact]
    public void MapLedger_HonoursTheTechnicalRoundingSpecifiedCompanion()
    {
        var summary = Summary(
            Allocated(Mrn, ItemChoiceType2.MRN, 300m, rounding: 0.25m),
            Allocated(Mrn, ItemChoiceType2.MRN, 100m)
        );

        var reserved = ChedQuantityMapper.MapLedger(summary).Allocations!.Reserved;

        reserved[0].TechnicalRoundingQuantity.Should().Be(0.25m);
        reserved[1].TechnicalRoundingQuantity.Should().BeNull("an unspecified rounding is not zero rounding");
    }

    [Fact]
    public void MapLedger_HonoursTheEventDateTimeSpecifiedCompanion()
    {
        var withDate = Allocated(Mrn, ItemChoiceType2.MRN, 1m);
        var withoutDate = Allocated(Mrn, ItemChoiceType2.MRN, 1m);
        withoutDate.EventDateTimeSpecified = false;

        var reserved = ChedQuantityMapper.MapLedger(Summary(withDate, withoutDate)).Allocations!.Reserved;

        reserved[0].EventDateTime.Should().Be(new DateTimeOffset(2026, 3, 4, 9, 15, 0, TimeSpan.Zero));
        reserved[1].EventDateTime.Should().BeNull();
    }

    [Theory]
    [InlineData("7", 7)]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("not-a-number", null)]
    // xs:integer is unbounded, so a value beyond int is valid upstream and must not throw here.
    [InlineData("99999999999999999999", null)]
    public void MapLedger_ParsesXsIntegerStringsWithoutThrowing(string? source, int? expected)
    {
        var allocation = Allocated(Mrn, ItemChoiceType2.MRN, 1m);
        allocation.GoodsItemNumber = source;

        var reserved = ChedQuantityMapper.MapLedger(Summary(allocation)).Allocations!.Reserved;

        reserved.Single().GoodsItemNumber.Should().Be(expected);
    }

    /// <summary>
    /// Guards R4. The request and response choice enums both default to <c>LRN</c> at index 0, so
    /// the discriminator has to come from <c>ItemElementName</c> rather than from the value. This is
    /// the only thing stopping a consumer that filters on <c>declarationReference</c> from reading
    /// an LRN as the MRN it happens to match.
    /// </summary>
    [Fact]
    public void MapLedger_DiscriminatesMrnFromLrn()
    {
        var summary = Summary(
            Allocated(Mrn, ItemChoiceType2.MRN, 1m),
            Allocated(Mrn, ItemChoiceType2.LRN, 2m),
            Allocated(null, ItemChoiceType2.MRN, 3m)
        );

        var reserved = ChedQuantityMapper.MapLedger(summary).Allocations!.Reserved;

        reserved[0]
            .DeclarationReference.Should()
            .Be(new DeclarationReference { Type = DeclarationReferenceType.Mrn, Value = Mrn });
        reserved[1].DeclarationReference!.Type.Should().Be(DeclarationReferenceType.Lrn);
        reserved[2].DeclarationReference.Should().BeNull("there is no reference without a value");
    }

    /// <summary>
    /// The ledger carries every declaration's allocations, discriminated by
    /// <c>declarationReference</c>. There is no per-declaration endpoint; consumers narrow to one
    /// MRN themselves, which is only sound because the mapper never promotes an LRN to an MRN.
    /// </summary>
    [Fact]
    public void MapLedger_CarriesAllocationsForEveryDeclaration()
    {
        var summary = new QuantityManagementCommoditySummaryEnhanced4ChedR51Type
        {
            ReservedQuantity =
            [
                Allocated(Mrn, ItemChoiceType2.MRN, 300m),
                Allocated("26GB99WTYXQ2LM5BC7", ItemChoiceType2.MRN, 90m),
            ],
            ConsumedQuantity = [Allocated(Mrn, ItemChoiceType2.MRN, 120m)],
        };

        var allocations = ChedQuantityMapper.MapLedger(summary).Allocations!;

        allocations.Reserved.Select(r => r.DeclarationReference!.Value).Should().Equal(Mrn, "26GB99WTYXQ2LM5BC7");
        allocations.Consumed.Should().ContainSingle().Which.Quantity.Should().Be(120m);
    }

    private static QuantityManagementCommoditySummaryEnhanced4ChedR51Type Summary(
        params AllocatedProductQuantityByCustomsOfficeEnhanced4ChedR51Type[] reserved
    ) => new() { ReservedQuantity = reserved };

    private static AllocatedProductQuantityByCustomsOfficeEnhanced4ChedR51Type Allocated(
        string? reference,
        ItemChoiceType2 referenceType,
        decimal quantity,
        decimal? rounding = null
    ) =>
        new()
        {
            GoodsItemNumber = "1",
            SwSupportingDocument = new SWSupportingWRoundingDocumentType
            {
                UnitOfMeasure = UniversalUnitOfMeasureType.KGM,
                Quantity = new SWSupportingWRoundingDocumentTypeQuantity
                {
                    Value = quantity,
                    TechnicalRoundingQuantity = rounding ?? 0m,
                    TechnicalRoundingQuantitySpecified = rounding.HasValue,
                },
                CertificateLineNumber = "1",
            },
            EventDateTime = new DateTime(2026, 3, 4, 9, 15, 0, DateTimeKind.Utc),
            EventDateTimeSpecified = true,
            CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = "GB000060" },
            Item = reference,
            ItemElementName = referenceType,
        };
}
