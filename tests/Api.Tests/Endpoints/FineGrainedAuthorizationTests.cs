using System.Net;
using AwesomeAssertions;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class FineGrainedAuthorizationTests(TradeGatewayWebApplicationFactory factory)
{
    private const string IntraInstancePath = "/certificates/intras/AUTHZ1";
    private const string IntraCollectionPath = "/certificates/intras";
    private const string ReferenceDataPath = "/reference-data/classifications/sections";
    private const string ChedPath = "/certificates/cheds/CHEDA.XI.2026.0000063";
    private const string CustomsQuantitiesPath = "/customs/cheds/CHEDA.GB.2026.0000123/quantities";

    [Fact]
    public async Task IntraReader_can_read_intra()
    {
        StubIntraGet();
        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");

        var response = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IntraReader_cannot_read_reference_data()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");

        var response = await client.GetAsync(ReferenceDataPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReferenceDataReader_can_read_reference_data()
    {
        StubClassificationSections();
        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetAsync(ReferenceDataPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReferenceDataReader_cannot_read_intra()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InstanceReader_can_read_instance_but_not_collection()
    {
        StubIntraGet();
        var client = await factory.CreateClientForPrincipalAsync("test-intra-instance-reader");

        var instance = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);
        instance.StatusCode.Should().Be(HttpStatusCode.OK);

        var collection = await client.GetAsync(IntraCollectionPath, TestContext.Current.CancellationToken);
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

        var response = await client.GetAsync(CustomsQuantitiesPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustomsQuantityReader_can_read_customs_quantities()
    {
        StubCustomsQuantities();
        var client = await factory.CreateClientForPrincipalAsync("test-customs-quantity-reader");

        var response = await client.GetAsync(CustomsQuantitiesPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CustomsQuantityReader_cannot_read_ched_certificate()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-customs-quantity-reader");

        var response = await client.GetAsync(ChedPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_principal_with_no_config_entry_is_forbidden()
    {
        var client = await factory.CreateClientForPrincipalAsync("not-a-configured-principal");

        var response = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task No_token_is_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

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
