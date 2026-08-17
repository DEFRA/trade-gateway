using System.Net;
using AwesomeAssertions;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class CustomsChedReleaseEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string ProcessedChedSoapAction =
        "\"http://ec.europa.eu/tracesnt/ws/impl/customs_certex/ched/v06/CustomsCertexChedPort/ChedClearanceRequest\"";

    private const string Ched = "CHEDA.GB.2026.0000123";
    private const string Mrn = "26GB16RF3TDPZE7AR2";
    private const string Manager = "test-customs-quantity-manager";

    private const string ReleasedSample = "Api.Tests.Samples.CUSTOMS.ReleaseChedResponse_Released.xml";
    private const string NotReleasedSample = "Api.Tests.Samples.CUSTOMS.ReleaseChedResponse_NotReleased.xml";


    [Fact]
    public async Task Put_ReleaseReservationSuccess()
    {
        StubSample(Ched, ReleasedSample);

        var response = await PutAsync(Ched, Mrn);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_ReleaseReservationFailed()
    {
        StubSample(Ched, NotReleasedSample);

        var response = await PutAsync(Ched, Mrn);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<HttpResponseMessage> PutAsync(
        string chedId,
        string mrn
    )
    {
        var client = await factory.CreateClientForPrincipalAsync(Manager);
        return await client.ReleaseChedReservation(chedId, mrn, TestContext.Current.CancellationToken);
    }

    private void StubSample(string chedId, string resourceName) =>
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    ProcessedChedSoapAction,
                    $"/*[local-name() = 'ChedClearanceRequest']/*[local-name() = 'ChedCertificateId' and text() = '{chedId}']"
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
