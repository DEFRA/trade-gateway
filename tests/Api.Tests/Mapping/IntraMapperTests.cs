using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Tests.Mapping;

public class IntraMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_SetsConstModelAndType()
    {
        var result = IntraMapper.Map(MinimalCertificate(), Context);

        result.Model.Should().Be("defra/certificate-internal/1");
        result.Type.Should().Be("intra");
    }

    [Fact]
    public void Map_LaboratoryObservationResult_IsNull_WhenSourceHasNone()
    {
        IntraMapper.Map(MinimalCertificate(), Context).LaboratoryObservationResult.Should().BeNull();
    }

    [Fact]
    public void Map_LaboratoryObservationResult_MapsFromConsignmentItemLaboratoryTests()
    {
        var cert = MinimalCertificate();
        cert.SPSConsignmentItemLaboratoryTest =
        [
            new SPSConsignmentItemLaboratoryTestType
            {
                NatureIdentificationSPSCargo = new SPSCargoType
                {
                    TypeCode = new CargoTypeClassificationCodeType
                    {
                        Value = CargoTypeClassificationCodeContentType.Item12,
                    },
                },
                ProductSPSLaboratoryTest =
                [
                    new ProductSPSLaboratoryTestType
                    {
                        ProductSPSClassification = new SPSClassificationType
                        {
                            SystemID = new IDType { Value = "CN" },
                            SystemName = [new TextType { Value = "CN Code (Combined Nomenclature)" }],
                            ClassName = [new TextType { Value = "LIVE ANIMALS" }],
                        },
                        SPSLaboratoryTest =
                        [
                            new SPSLaboratoryTestType
                            {
                                Reference = "LAP-000000915-INTRA.EU.NL.2021.0000001",
                                TestDescriptor = new LaboratoryTestDescriptorType
                                {
                                    ID = new IDType { Value = "11221" },
                                    Description = new TextType { Value = "ANTICOCCIDIALS, INCLUDING NITROIMIDAZOLES" },
                                    CategoryCode = new CodeType { Value = "RESIDUES_B2B" },
                                },
                                TestMotivationCode = new CodeType { Value = "RANDOM" },
                                InspectorConclusionCode = new CodeType { Value = "SATISFACTORY" },
                                Analysys =
                                [
                                    new SPSLaboratoryTestAnalysisType
                                    {
                                        AnalysisTypeCode = new CodeType { Value = "INITIAL" },
                                        SamplingDateTime = new DateTime(2021, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                                        SamplingDateTimeSpecified = true,
                                        SampleBatchNumber = "1223",
                                        NumberOfSamples = "2",
                                        SampleTypeCode = new CodeType { Value = "LARVA" },
                                        SampleConservationCode = new CodeType { Value = "CHILLED" },
                                        LaboratorySPSParty = new SPSPartyType
                                        {
                                            ID = new IDType { Value = "NLCODE123" },
                                            Name = new TextType { Value = "Test Lab in NL" },
                                        },
                                        LaboratoryReceiptDateTime = new DateTime(2021, 3, 2, 0, 0, 0, DateTimeKind.Utc),
                                        LaboratoryReceiptDateTimeSpecified = true,
                                        LaboratoryReportDateTime = new DateTime(2021, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                                        LaboratoryReportDateTimeSpecified = true,
                                        LaboratoryTestMethod = "test met",
                                        LaboratoryResults = "ok",
                                        LaboratoryConclusionCode = new CodeType { Value = "SATISFACTORY" },
                                    },
                                ],
                            },
                        ],
                    },
                ],
            },
        ];

        var result = IntraMapper.Map(cert, Context).LaboratoryObservationResult.Should().ContainSingle().Subject;

        result.NatureIdCargo!.TypeCode.Should().Be("12");

        var product = result.ProductLaboratoryTest.Should().ContainSingle().Subject;
        product.ApplicableProductClassification!.SystemId.Should().Be("CN");

        var test = product.LaboratoryTest.Should().ContainSingle().Subject;
        test.Reference.Should().Be("LAP-000000915-INTRA.EU.NL.2021.0000001");
        test.TestDescriptor!.Id.Should().Be(11221);
        test.TestDescriptor.Description.Should().Be("ANTICOCCIDIALS, INCLUDING NITROIMIDAZOLES");
        test.TestDescriptor.CategoryCode.Should().Be("RESIDUES_B2B");
        test.TestMotivationCode.Should().Be("RANDOM");
        test.InspectorConclusionCode.Should().Be("SATISFACTORY");

        var analysis = test.Analysis!;
        analysis.AnalysisTypeCode.Should().Be("INITIAL");
        analysis.SamplingDateTime.Should().Be(new DateTimeOffset(2021, 3, 9, 0, 0, 0, TimeSpan.Zero));
        analysis.SampleBatchNumber.Should().Be(1223);
        analysis.NumberOfSamples.Should().Be(2);
        analysis.SampleTypeCode.Should().Be("LARVA");
        analysis.SampleConservationCode.Should().Be("CHILLED");
        analysis.Laboratory!.Identifier.Should().Be("NLCODE123");
        analysis.Laboratory.Name.Should().Be("Test Lab in NL");
        analysis.LaboratoryReceiptDateTime.Should().Be(new DateTimeOffset(2021, 3, 2, 0, 0, 0, TimeSpan.Zero));
        analysis.LaboratoryReportDateTime.Should().Be(new DateTimeOffset(2021, 3, 9, 0, 0, 0, TimeSpan.Zero));
        analysis.LaboratoryTestMethod.Should().Be("test met");
        analysis.LaboratoryResults.Should().Be("ok");
        analysis.LaboratoryConclusionCode.Should().Be("SATISFACTORY");
    }

    [Fact]
    public void Map_SpecifiedConsignment_IsNotNull()
    {
        var result = IntraMapper.Map(MinimalCertificate(), Context);

        result.SpecifiedConsignment.Should().NotBeNull();
    }

    [Fact]
    public void Map_ExchangedDocumentTypeCode_MapsFromSPSExchangedDocument()
    {
        IntraMapper.Map(MinimalCertificate(), Context).ExchangedDocument.DocumentTypeCode.Should().Be("856");
    }

    [Fact]
    public void ToDefraUNVTDINTRAProfile_ExtensionMethod_ProducesSameResult()
    {
        var cert = MinimalCertificate();

        IntraMapper.Map(cert, Context).Should().BeEquivalentTo(cert.ToDefraUNVTDINTRAProfile(Context));
    }

    [Fact]
    public async Task ToDefraUNVTDINTRAProfileSummary_Maps_All_Properties()
    {
        // Arrange
        var created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var source = new FindEuIntraCertificateResultType
        {
            offset = 10,
            pageSize = 2,
            EuIntraCertificateResult =
            [
                new EuIntraCertificateQueryResultType
                {
                    ID = "CERT123",
                    CreateDateTime = created,
                    UpdateDateTime = updated,
                    CountryOfOrigin = [new IDType() { Value = "GB" }],
                },
            ],
        };

        // Act
        var result = IntraMapper.Map(source);

        // Assert
        result.Should().NotBeNull();
        await Verify(result);
    }

    [Fact]
    public void ToDefraUNVTDINTRAProfileSummary_NullResults_ReturnsEmptyItems()
    {
        var source = new FindEuIntraCertificateResultType
        {
            offset = 0,
            pageSize = 10,
            EuIntraCertificateResult = null,
        };

        var result = IntraMapper.Map(source);

        result.Items.Should().NotBeNull().And.BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    private static EuIntraCertificateType MinimalCertificate() =>
        new()
        {
            SPSCertificate = new SPSCertificateType
            {
                SPSExchangedDocument = new SPSExchangedDocumentType
                {
                    ID = new IDType { Value = "DOC-1" },
                    TypeCode = new DocumentCodeType { Value = DocumentNameCodeContentType.Item856 },
                },
                SPSConsignment = new SPSConsignmentType(),
            },
        };
}
