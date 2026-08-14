using System.Net;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Customs;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class FineGrainedAuthorizationTests(TradeGatewayWebApplicationFactory factory)
{
    [Fact]
    public async Task IntraReader_can_read_intra()
    {
        StubIntraGet();
        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");

        var response = await client.GetIntraCertification("AUTHZ1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IntraReader_cannot_read_reference_data()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");

        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReferenceDataReader_can_read_reference_data()
    {
        StubClassificationSections();
        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReferenceDataReader_cannot_read_intra()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetIntraCertification("AUTHZ1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InstanceReader_can_read_instance_but_not_collection()
    {
        StubIntraGet();
        var client = await factory.CreateClientForPrincipalAsync("test-intra-instance-reader");

        var instance = await client.GetIntraCertification("AUTHZ1", TestContext.Current.CancellationToken);
        instance.StatusCode.Should().Be(HttpStatusCode.OK);

        var collection = await client.FindIntraUpdates(
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue,
            10,
            0,
            TestContext.Current.CancellationToken);
        collection.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// The load-bearing test for the URL shape: <c>customs/</c> is a sibling of <c>certificates/</c>
    /// precisely so that the <c>ched-reader</c> grant on <c>/certificates/cheds/**</c> cannot reach
    /// customs quantity data.
    /// </summary>
    [Fact]
    public async Task ChedReader_cannot_read_customs_quantities()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");

        var response = await client.GetChedQuantities("CHEDA.GB.2026.0000123", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustomsQuantityReader_can_read_customs_quantities()
    {
        StubCustomsQuantities();
        var client = await factory.CreateClientForPrincipalAsync("test-customs-quantity-reader");

        var response = await client.GetChedQuantities("CHEDA.GB.2026.0000123", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CustomsQuantityReader_cannot_read_ched_certificate()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-customs-quantity-reader");

        var response = await client.GetChedCertification("CHEDA.XI.2026.0000063", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustomsQuantityReader_cannot_write_reservation()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-customs-quantity-reader");

        var response = await client.PutChedReservation(
            "CHEDA.GB.2026.0000123",
            "26GB16RF3TDPZE7AR2",
            ReservationRequest,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustomsQuantityManager_can_write_reservation()
    {
        StubCustomsReservation();
        var client = await factory.CreateClientForPrincipalAsync("test-customs-quantity-manager");

        var response = await client.PutChedReservation(
            "CHEDA.GB.2026.0000123",
            "26GB16RF3TDPZE7AR2",
            ReservationRequest,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChedReader_cannot_write_customs_reservation()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");

        var response = await client.PutChedReservation(
            "CHEDA.GB.2026.0000123",
            "26GB16RF3TDPZE7AR2",
            ReservationRequest,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_principal_with_no_config_entry_is_forbidden()
    {
        var client = await factory.CreateClientForPrincipalAsync("not-a-configured-principal");

        var response = await client.GetIntraCertification("AUTHZ1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task No_token_is_unauthorized()
    {
        var response = await factory.CreateITracesGatewayClient().GetIntraCertification("AUTHZ1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_is_accessible_without_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void StubIntraGet() =>
        factory.WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                "\"getEuIntraCertificate\"",
                "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
            .RespondWith(Response.Create().WithCallback(async _ =>
                await SoapUtilities.CreateResponseFromResource(
                    HttpStatusCode.OK,
                    "Api.Tests.Samples.INTRA.GetEuIntraCertificateResponse.xml")));

    private static ChedReservationRequest ReservationRequest =>
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

    private void StubCustomsReservation() =>
        factory.WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                "\"http://ec.europa.eu/tracesnt/ws/impl/customs_certex/ched/v06/CustomsCertexChedPort/ProcessedChedRequest\"",
                "/*[local-name() = 'ProcessedChedRequest']/*[local-name() = 'ChedCertificateId' and text() = 'CHEDA.GB.2026.0000123']"))
            .RespondWith(Response.Create().WithCallback(async _ =>
                await SoapUtilities.CreateResponseFromResource(
                    HttpStatusCode.OK,
                    "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_Reserved.xml")));

    private void StubCustomsQuantities() =>
        factory.WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                "\"http://ec.europa.eu/tracesnt/ws/impl/customs_certex/ched/v06/CustomsCertexChedPort/ProcessedChedRequest\"",
                "/*[local-name() = 'ProcessedChedRequest']/*[local-name() = 'ChedCertificateId' and text() = 'CHEDA.GB.2026.0000123']"))
            .RespondWith(Response.Create().WithCallback(async _ =>
                await SoapUtilities.CreateResponseFromResource(
                    HttpStatusCode.OK,
                    "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_CHEDA.GB.2026.0000123.xml")));

    private void StubClassificationSections() =>
        factory.WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                "\"getClassificationSections\"",
                "/*[local-name() = 'GetClassificationSectionsRequest']"))
            .RespondWith(Response.Create().WithCallback(async _ =>
                await SoapUtilities.CreateResponseFromResource(
                    HttpStatusCode.OK,
                    "Api.Tests.Samples.REFERENCE_DATA.GetClassificationSectionsResponse.xml")));
}
