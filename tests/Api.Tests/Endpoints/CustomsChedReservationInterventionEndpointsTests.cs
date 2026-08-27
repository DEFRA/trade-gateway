using System.Net;
using System.Xml.Linq;
using Api.Contract;
using AwesomeAssertions;
using Refit;
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
        "Api.Tests.Samples.CUSTOMS.ReservationInterventionResponse_Unsuccessful.xml";

    private static ChedReservationInterventionRequest Request =>
        new()
        {
            ChedCertificateId = Ched,
            CompetentCustomsOffice = new CompetentCustomsOffice() { ReferenceNumber = "GBTEST01" },
            CustomsDocumentReference = "GB12345678901234567890",
            InterventionType = InterventionType.PhysicalCheck,
            SendingDate = DateTime.Now,
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
    public async Task Put_ReservationInterventionSuccess()
    {
        StubSample(Ched, SuccessSample);

        var response = await PutAsync(Ched, Mrn, Request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_ReservationInterventionUnsuccessful()
    {
        StubSample(UnsuccessfulChed, UnsuccessfulSample);

        var response = await PutAsync(UnsuccessfulChed, Mrn, Request with { ChedCertificateId = UnsuccessfulChed });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Put_ReservationIntervention_WhenRequestIsNotValid_ReturnsBadRequest()
    {
        var request = Request with { ConsignmentItems = [] };

        var response = await PutAsync(Ched, Mrn, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Put_ReservationIntervention_WhenUpstreamFault_ReturnsBadGatewayWithoutTheUpstreamMessage()
    {
        const string upstreamError = "internal upstream detail that must not be published";
        StubFault("FAULTY", upstreamError);
        var request = Request with { ChedCertificateId = "FAULTY" };

        var response = await PutAsync("FAULTY", "mrn", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Put_ReservationIntervention_WhenSenderSaxFault_ReturnsInternalServerError()
    {
        StubSaxFault("BADSOAP");
        var request = Request with { ChedCertificateId = "BADSOAP" };

        var response = await PutAsync("BADSOAP", "mrn", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private async Task<HttpResponseMessage> PutAsync(
        string chedId,
        string mrn,
        ChedReservationInterventionRequest request
    )
    {
        var client = await factory.CreateClientForPrincipalAsync(Manager);
        return await client.ChedReservationIntervention(chedId, mrn, request, TestContext.Current.CancellationToken);
    }

    private void StubSample(string chedId, string resourceName) =>
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
                        await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, resourceName)
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
