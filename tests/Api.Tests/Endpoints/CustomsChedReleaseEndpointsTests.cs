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

    [Fact]
    public async Task Put_ReleaseReservation_WhenUpstreamFault_ReturnsBadGatewayWithoutTheUpstreamMessage()
    {
        const string upstreamError = "internal upstream detail that must not be published";
        StubFault("FAULTY", upstreamError);

        var response = await PutAsync("FAULTY", "mrn");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Put_ReleaseReservations_WhenSenderSaxFault_ReturnsInternalServerError()
    {
        StubSaxFault("BADSOAP");

        var response = await PutAsync("BADSOAP", "mrn");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private async Task<HttpResponseMessage> PutAsync(string chedId, string mrn)
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

    private void StubFault(string chedId, string upstreamError) =>
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
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.InternalServerError,
                            $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                              <soap:Body>
                                <soap:Fault>
                                  <faultcode>soap:Server</faultcode>
                                  <faultstring>Quantity management failed</faultstring>
                                  <detail>
                                    <ChedClearanceRequestFault xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/ched/v06">
                                      <MessageId xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">upstream-message-id</MessageId>
                                      <UniqPrefix xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">GBTEST01</UniqPrefix>
                                      <errorMessage xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">{upstreamError}</errorMessage>
                                    </ChedClearanceRequestFault>
                                  </detail>
                                </soap:Fault>
                              </soap:Body>
                            </soap:Envelope>
                            """
                        )
                    )
            );

    private void StubSaxFault(string chedId) =>
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
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.InternalServerError,
                            """
                            <?xml version="1.0" encoding="utf-8"?>
                            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                              <soap:Body>
                                <soap:Fault>
                                  <faultcode>soap:Client</faultcode>
                                  <faultstring>SAXException: unexpected element</faultstring>
                                </soap:Fault>
                              </soap:Body>
                            </soap:Envelope>
                            """
                        )
                    )
            );
}
