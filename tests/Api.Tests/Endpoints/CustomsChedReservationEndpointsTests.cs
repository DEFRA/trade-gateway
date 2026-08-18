using System.Net;
using System.Xml.Linq;
using Api.Contract;
using AwesomeAssertions;
using Refit;
using Trade.Gateway.Api.Contract.Customs;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class CustomsChedReservationEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string ProcessedChedSoapAction =
        "\"http://ec.europa.eu/tracesnt/ws/impl/customs_certex/ched/v06/CustomsCertexChedPort/ProcessedChedRequest\"";

    private const string Ched = "CHEDA.GB.2026.0000123";
    private const string Mrn = "26GB16RF3TDPZE7AR2";
    private const string Manager = "test-customs-quantity-manager";

    private const string ReservedSample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_Reserved.xml";
    private const string RefusedSample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_ReservationRefused.xml";
    private const string RefusedWithCodeSample =
        "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_ReservationRefusedWithCode.xml";
    private const string NoResultSample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_NoReservationResult.xml";
    private const string OtherDeclarationSample =
        "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_ReservedOtherDeclarationOnly.xml";
    private const string NoSummarySample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_NoSummary.xml";
    private const string UnknownChedSample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_UnknownChed.xml";

    private static ChedReservationRequest Request =>
        new()
        {
            Items =
            [
                new ReservationCommodityItem
                {
                    GoodsItemNumber = 1,
                    CertificateLineNumber = 1,
                    ClassCode = "P1",
                    NetWeightQuantity = 300m,
                    NetWeightUnitOfMeasure = "KGM",
                },
            ],
        };

    [Fact]
    public async Task Put_ReturnsTheDeclarationsReservation()
    {
        StubSample(Ched, ReservedSample);

        var response = await PutAsync(Ched, Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response
            .ContentHeaders?.ContentType?.MediaType.Should()
            .Be(MediaTypeAttribute.For<ChedDeclarationReservation>());
        await Verify(response.Content);
    }

    [Fact]
    public async Task Put_ExcludesOtherDeclarationsAndLrnsWithTheSameValue()
    {
        StubSample(Ched, ReservedSample);

        var response = await PutAsync(Ched, Mrn, Request);

        var reserved = response.Content!.Reserved;
        reserved.Should().ContainSingle();
        reserved[0].DeclarationReference!.Value.Should().Be(Mrn);
        reserved[0].DeclarationReference!.Type.Should().Be(DeclarationReferenceType.Mrn);
        // The sample's LRN carries the same characters as the MRN and reserves 777.
        reserved.Should().NotContain(r => r.Quantity == 777m);
    }

    [Fact]
    public async Task Put_SendsQuantityManagementIndicationOne()
    {
        StubSample(Ched, ReservedSample);

        await PutAsync(Ched, Mrn, Request);

        RequestBody()
            .Descendants()
            .Single(e => e.Name.LocalName == "QuantityManagementIndication")
            .Value.Should()
            .Be("1");
    }

    [Fact]
    public async Task Put_SendsTheMrnDiscriminatorNotAnLrn()
    {
        StubSample(Ched, ReservedSample);

        await PutAsync(Ched, Mrn, Request);

        var declarationReference = RequestBody()
            .Descendants()
            .Single(e => e.Name.LocalName == "CustomsDeclarationReferenceNumber");

        declarationReference.Elements().Select(e => e.Name.LocalName).Should().Equal("MRN");
        declarationReference.Elements().Single().Value.Should().Be(Mrn);
    }

    [Fact]
    public async Task Put_SendsQuantitiesWhoseSpecifiedCompanionsWereSet()
    {
        StubSample(Ched, ReservedSample);

        await PutAsync(Ched, Mrn, Request);

        var item = RequestBody().Descendants().Single(e => e.Name.LocalName == "CommodityDescriptionForChed");

        item.Elements().Single(e => e.Name.LocalName == "NetWeightQuantity").Value.Should().Be("300");
        item.Elements().Single(e => e.Name.LocalName == "NetWeightUnitOfMeasure").Value.Should().Be("KGM");
        // Volume was not supplied, so it must not be sent as a zero the caller never asked for.
        item.Elements().Should().NotContain(e => e.Name.LocalName == "NetVolumeQuantity");
    }

    [Fact]
    public async Task Put_WhenReservationRefused_ReturnsConflictWithoutTheUpstreamReason()
    {
        StubSample(Ched, RefusedSample);

        var response = await PutAsync(Ched, Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = (response.Error as ApiException)?.Content;
        body.Should().NotBeNull().And.NotContain("MUST NOT BE PUBLISHED");
        await Verify(body);
    }

    [Fact]
    public async Task Put_WhenReservationRefusedWithAKnownCode_ReturnsTheDecodedReason()
    {
        StubSample("REFUSEDWITHCODE", RefusedWithCodeSample);

        var response = await PutAsync("REFUSEDWITHCODE", Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await Verify((response.Error as ApiException)?.Content);
    }

    [Fact]
    public async Task Put_WhenReservationResultUnspecified_ReturnsBadGateway()
    {
        StubSample(Ched, NoResultSample);

        var response = await PutAsync(Ched, Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Put_WhenReservedButNoSummaryReturned_ReturnsBadGateway()
    {
        StubSample("NOSUMMARY", NoSummarySample);

        var response = await PutAsync("NOSUMMARY", Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Put_WhenNoAllocationMatchesTheMrn_ReturnsBadGateway()
    {
        StubSample("OTHERONLY", OtherDeclarationSample);

        var response = await PutAsync("OTHERONLY", Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Put_WhenChedNotFound_ReturnsNotFound()
    {
        StubSample("UNKNOWN", UnknownChedSample);

        var response = await PutAsync("UNKNOWN", Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_WhenNoItems_ReturnsBadRequest()
    {
        var response = await PutAsync(Ched, Mrn, new ChedReservationRequest { Items = [] });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_WhenUnitOfMeasureUnrecognised_ReturnsBadRequest()
    {
        var request = new ChedReservationRequest
        {
            Items =
            [
                new ReservationCommodityItem
                {
                    GoodsItemNumber = 1,
                    CertificateLineNumber = 1,
                    ClassCode = "P1",
                    NetWeightQuantity = 10m,
                    NetWeightUnitOfMeasure = "KILOS",
                },
            ],
        };

        var response = await PutAsync(Ched, Mrn, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_WhenQuantityHasNoUnitOfMeasure_ReturnsBadRequest()
    {
        var request = new ChedReservationRequest
        {
            Items =
            [
                new ReservationCommodityItem
                {
                    GoodsItemNumber = 1,
                    CertificateLineNumber = 1,
                    ClassCode = "P1",
                    NetWeightQuantity = 10m,
                },
            ],
        };

        var response = await PutAsync(Ched, Mrn, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// A field TracesNT requires but the request pipeline does not check: without the validator the
    /// request reaches TracesNT as a schema violation, which comes back to the caller as a 500. The
    /// problem names the offending item, as FluentValidation spells the property.
    /// </summary>
    [Theory]
    [InlineData("GoodsItemNumber")]
    [InlineData("CertificateLineNumber")]
    [InlineData("ClassCode")]
    public async Task Put_WhenARequiredItemFieldIsMissing_ReturnsBadRequestNamingIt(string property)
    {
        StubSample(Ched, ReservedSample);

        var request = new ChedReservationRequest
        {
            Items =
            [
                new ReservationCommodityItem
                {
                    GoodsItemNumber = property == "GoodsItemNumber" ? null : 1,
                    CertificateLineNumber = property == "CertificateLineNumber" ? null : 1,
                    ClassCode = property == "ClassCode" ? null : "P1",
                    NetWeightQuantity = 300m,
                    NetWeightUnitOfMeasure = "KGM",
                },
            ],
        };

        var response = await PutAsync(Ched, Mrn, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (response.Error as ApiException)?.Content.Should().Contain($"Items[0].{property}");
    }

    private async Task<ApiResponse<ChedDeclarationReservation>> PutAsync(
        string chedId,
        string mrn,
        ChedReservationRequest request
    )
    {
        var client = await factory.CreateClientForPrincipalAsync(Manager);
        return await client.PutChedReservation(chedId, mrn, request, TestContext.Current.CancellationToken);
    }

    private XElement RequestBody()
    {
        var body = factory
            .WireMockServer.LogEntries.Select(entry => entry.RequestMessage!.Body)
            .Last(body => body is not null && body.Contains("ProcessedChedRequest", StringComparison.Ordinal));

        return XDocument.Parse(body!).Descendants().Single(e => e.Name.LocalName == "ProcessedChedRequest");
    }

    private void StubSample(string chedId, string resourceName) =>
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    ProcessedChedSoapAction,
                    $"/*[local-name() = 'ProcessedChedRequest']/*[local-name() = 'ChedCertificateId' and text() = '{chedId}']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, resourceName)
                    )
            );
}
