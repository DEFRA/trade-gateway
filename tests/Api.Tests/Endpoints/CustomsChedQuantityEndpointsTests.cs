using System.Net;
using Api.Contract;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Customs;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class CustomsChedQuantityEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    /// <summary>
    /// WCF sends the full operation URI from the generated <c>OperationContract</c>, not the short
    /// form the CHED port uses.
    /// </summary>
    private const string ProcessedChedSoapAction =
        "\"http://ec.europa.eu/tracesnt/ws/impl/customs_certex/ched/v06/CustomsCertexChedPort/ProcessedChedRequest\"";

    private const string Ched = "CHEDA.GB.2026.0000123";
    private const string Principal = "test-customs-quantity-reader";

    private const string FullLedgerSample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_CHEDA.GB.2026.0000123.xml";
    private const string NoSummarySample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_NoSummary.xml";
    private const string UnknownChedSample = "Api.Tests.Samples.CUSTOMS.ProcessedChedResponse_UnknownChed.xml";

    [Fact]
    public async Task GetQuantities_ReturnsMappedLedger()
    {
        StubSample(Ched, FullLedgerSample);

        var response = await GetAsync($"/customs/cheds/{Ched}/quantities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response
            .Content.Headers.ContentType?.MediaType.Should()
            .Be(MediaTypeAttribute.For<ChedQuantityLedger>());
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// TracesNT answers for an unknown CHED with a successful response carrying no
    /// <c>ChedCertificate</c>. That is the only not-found signal the port offers — its single
    /// untyped fault says nothing — so it is what licenses a 404 here.
    /// </summary>
    [Fact]
    public async Task GetQuantities_WhenChedNotFound_ReturnsNotFound()
    {
        StubSample("UNKNOWN", UnknownChedSample);

        var response = await GetAsync("/customs/cheds/UNKNOWN/quantities");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    /// <summary>
    /// The CHED exists — TracesNT returned its certificate — but reported no quantity position for
    /// it. Distinct from the 404 above, and the distinction is the whole point: 404 says the CHED
    /// does not exist, 502 says we could not learn its position.
    /// </summary>
    [Fact]
    public async Task GetQuantities_WhenChedExistsButSummaryMissing_ReturnsBadGateway()
    {
        StubSample("NOSUMMARY", NoSummarySample);

        var response = await GetAsync("/customs/cheds/NOSUMMARY/quantities");

        // Never 200 with an empty ledger: absent and empty are identical on the wire, so an empty
        // success would assert "nothing is reserved" on no evidence. And never 404 — the CHED is
        // demonstrably there.
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetQuantities_WhenUpstreamFault_ReturnsBadGatewayWithoutTheUpstreamMessage()
    {
        const string upstreamError = "internal upstream detail that must not be published";
        StubFault("FAULTY", upstreamError);

        var response = await GetAsync("/customs/cheds/FAULTY/quantities");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContain(upstreamError, "upstream error text must reach logs only (ADR-0002 §4)");
    }

    [Fact]
    public async Task GetQuantities_WhenSenderSaxFault_ReturnsInternalServerError()
    {
        StubSaxFault("BADSOAP");

        var response = await GetAsync("/customs/cheds/BADSOAP/quantities");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private async Task<HttpResponseMessage> GetAsync(string path)
    {
        var client = await factory.CreateClientForPrincipalAsync(Principal);
        return await client.GetAsync(path, TestContext.Current.CancellationToken);
    }

    private void StubSample(string chedId, string resourceName) =>
        Given(chedId)
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, resourceName)
                    )
            );

    private void StubFault(string chedId, string upstreamError) =>
        Given(chedId)
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
                                    <ProcessedChedRequestFault xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/ched/v06">
                                      <MessageId xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">upstream-message-id</MessageId>
                                      <UniqPrefix xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">GBTEST01</UniqPrefix>
                                      <errorMessage xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">{upstreamError}</errorMessage>
                                    </ProcessedChedRequestFault>
                                  </detail>
                                </soap:Fault>
                              </soap:Body>
                            </soap:Envelope>
                            """
                        )
                    )
            );

    private void StubSaxFault(string chedId) =>
        Given(chedId)
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

    private WireMock.Server.IRespondWithAProvider Given(string chedId) =>
        factory.WireMockServer.Given(
            SoapUtilities.CreateSoapRequestInterceptor(
                ProcessedChedSoapAction,
                $"/*[local-name() = 'ProcessedChedRequest']/*[local-name() = 'ChedCertificateId' and text() = '{chedId}']"
            )
        );
}
