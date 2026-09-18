using System.Net;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Customs;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class CustomsChedReservationInterventionEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string ProcessedChedSoapAction =
        "\"http://ec.europa.eu/tracesnt/ws/impl/customs_certex/ched/v06/CustomsCertexChedPort/ChedInterventionRequest\"";

    private const string Ched = "CHEDA.GB.2026.0000123";
    private const string UnsuccessfulChed = "UNSUCCESSFUL";
    private const string Mrn = "26GB16RF3TDPZE7AR2";
    private const string Manager = "test-customs-quantity-manager";

    private const string SuccessSample = "Api.Tests.Samples.CUSTOMS.ReservationInterventionResponse_Success.xml";
    private const string UnsuccessfulSample =
        "Api.Tests.Samples.CUSTOMS.ReservationInterventionResponse_Unsuccessful_{{OutcomeCode}}.xml";

    private static ChedReservationInterventionRequest Request =>
        new()
        {
            TaricDocument = "GB12345678901234567890",
            ConsignmentItems =
            [
                new CustomsConsignmentItem()
                {
                    CertificateLineNumber = 1,
                    ClassCode = "P1",
                    GoodsItemNumber = 1,
                    NetVolumeQuantity = 100m,
                    NetVolumeUnitOfMeasure = UnitOfMeasureType.LTR,
                },
            ],
        };

    [Fact]
    public async Task ForceWriteOff_ReservationInterventionSuccess()
    {
        StubSample(Ched, SuccessSample);

        var response = await ForceWriteOffAsync(Ched, Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("02", HttpStatusCode.NotFound)]
    [InlineData("03", HttpStatusCode.Conflict)]
    [InlineData("05", HttpStatusCode.Conflict)]
    [InlineData("06", HttpStatusCode.BadGateway)]
    public async Task ForceWriteOff_ReservationInterventionUnsuccessful(
        string outcomeCode,
        HttpStatusCode expectedStatusCode
    )
    {
        StubSample(
            UnsuccessfulChed,
            UnsuccessfulSample,
            new TokenSubstitution { Token = "{{OutcomeCode}}", Substitution = outcomeCode }
        );

        var response = await ForceWriteOffAsync(UnsuccessfulChed, Mrn, Request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async Task UpdateWriteOff_ReservationInterventionSuccess()
    {
        StubSample(Ched, SuccessSample);

        var response = await UpdateWriteOff(Ched, Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("02", HttpStatusCode.NotFound)]
    [InlineData("03", HttpStatusCode.Conflict)]
    [InlineData("05", HttpStatusCode.Conflict)]
    [InlineData("06", HttpStatusCode.BadGateway)]
    public async Task UpdateWriteOff_ReservationInterventionUnsuccessful(
        string outcomeCode,
        HttpStatusCode expectedStatusCode
    )
    {
        StubSample(
            UnsuccessfulChed,
            UnsuccessfulSample,
            new TokenSubstitution { Token = "{{OutcomeCode}}", Substitution = outcomeCode }
        );

        var response = await UpdateWriteOff(UnsuccessfulChed, Mrn, Request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async Task DeleteWriteOff_ReservationInterventionSuccess()
    {
        StubSample(Ched, SuccessSample);

        var response = await DeleteWriteOffAsync(Ched, Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("02", HttpStatusCode.NotFound)]
    [InlineData("03", HttpStatusCode.Conflict)]
    [InlineData("05", HttpStatusCode.Conflict)]
    [InlineData("06", HttpStatusCode.BadGateway)]
    public async Task DeleteWriteOff_ReservationInterventionUnsuccessful(
        string outcomeCode,
        HttpStatusCode expectedStatusCode
    )
    {
        StubSample(
            UnsuccessfulChed,
            UnsuccessfulSample,
            new TokenSubstitution { Token = "{{OutcomeCode}}", Substitution = outcomeCode }
        );

        var response = await DeleteWriteOffAsync(UnsuccessfulChed, Mrn, Request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async Task ForceWriteOff_WhenRequestIsNotValid_ReturnsBadRequest()
    {
        var request = Request with { ConsignmentItems = [] };

        var response = await ForceWriteOffAsync(Ched, Mrn, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ForceWriteOff_WhenUpstreamFault_ReturnsBadGatewayWithoutTheUpstreamMessage()
    {
        const string upstreamError = "internal upstream detail that must not be published";
        StubFault("FAULTY", upstreamError);

        var response = await ForceWriteOffAsync("FAULTY", "mrn", Request);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ForceWriteOff_WhenSenderSaxFault_ReturnsInternalServerError()
    {
        StubSaxFault("BADSOAP");

        var response = await ForceWriteOffAsync("BADSOAP", "mrn", Request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private async Task<HttpResponseMessage> ForceWriteOffAsync(
        string chedId,
        string mrn,
        ChedReservationInterventionRequest request
    )
    {
        var client = await factory.CreateClientForPrincipalAsync(Manager);
        return await client.ForceReleaseChed(chedId, mrn, request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> UpdateWriteOff(
        string chedId,
        string mrn,
        ChedReservationInterventionRequest request
    )
    {
        var client = await factory.CreateClientForPrincipalAsync(Manager);
        return await client.UpdateForceReleaseChed(chedId, mrn, request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> DeleteWriteOffAsync(
        string chedId,
        string mrn,
        ChedReservationInterventionRequest request
    )
    {
        var client = await factory.CreateClientForPrincipalAsync(Manager);
        return await client.DeleteForceReleaseChed(chedId, mrn, request, TestContext.Current.CancellationToken);
    }

    private void StubSample(string chedId, string resourceName, params TokenSubstitution[] resourceSubstitutions) =>
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    ProcessedChedSoapAction,
                    $"/*[local-name() = 'ChedInterventionRequest']/*[local-name() = 'ChedCertificateId' and text() = '{chedId}']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            resourceName,
                            resourceSubstitutions
                        )
                    )
            );

    private void StubFault(string chedId, string upstreamError) =>
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    ProcessedChedSoapAction,
                    $"/*[local-name() = 'ChedInterventionRequest']/*[local-name() = 'ChedCertificateId' and text() = '{chedId}']"
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
                    $"/*[local-name() = 'ChedInterventionRequest']/*[local-name() = 'ChedCertificateId' and text() = '{chedId}']"
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
