using System.Net;
using System.Net.Http.Json;
using Api.Contract;
using Refit;
using Trade.Gateway.Api.Contract.Certificate;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class ChedEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string GetChedCertificateSoapAction = "\"getChedCertificate\"";
    private const string FindChedCertificateSoapAction = "\"findChedCertificate\"";

    private static string ChedRequestXPath(string chedId) =>
        $"/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = '{chedId}']";

    private const string ChedSampleResource = "Api.Tests.Samples.CHED.GetChedResponse_CHEDA.XI.2026.0000063.xml";

    [Fact]
    public async Task Get_ReturnsMappedDefraUNVTDCHEDProfile()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    ChedRequestXPath("CHEDA.XI.2026.0000063")
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, ChedSampleResource)
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification(
            "CHEDA.XI.2026.0000063",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(MediaTypeAttribute.For<DefraUNVTDCHEDProfile>(), response.ContentHeaders?.ContentType?.MediaType);
        await Verify(response.Content);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'BADSOAP']"
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
                            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              <s:Body>
                                <s:Fault>
                                  <faultcode>s:Client</faultcode>
                                  <faultstring>SAXException: invalid request</faultstring>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("BADSOAP", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'COMMFAIL']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("COMMFAIL", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'MISSING']"
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
                            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              <s:Body>
                                <s:Fault>
                                  <faultcode>s:Client</faultcode>
                                  <faultstring>Certificate not found</faultstring>
                                  <detail>
                                    <ChedCertificateNotFoundException xmlns="http://ec.europa.eu/tracesnt/certificate/ched/v2">
                                      <CertificateIdentifier>MISSING</CertificateIdentifier>
                                    </ChedCertificateNotFoundException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("MISSING", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
        await Verify((response.Error as ValidationApiException)?.Content);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsPermissionDeniedFault_ReturnsForbidden()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'FORBIDDEN']"
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
                            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              <s:Body>
                                <s:Fault>
                                  <faultcode>s:Client</faultcode>
                                  <faultstring>Permission denied</faultstring>
                                  <detail>
                                    <ChedCertificatePermissionDeniedException xmlns="http://ec.europa.eu/tracesnt/certificate/ched/v2">
                                      <CertificateIdentifier>FORBIDDEN</CertificateIdentifier>
                                    </ChedCertificatePermissionDeniedException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("FORBIDDEN", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    /// <summary>
    /// Stubs getChedCertificate for <c>DRAFTCHED</c> with the sample CHED marked draft by its status code.
    /// </summary>
    private void StubDraftChed() =>
        SoapUtilities.StubDraftCertificate(
            factory.WireMockServer,
            GetChedCertificateSoapAction,
            ChedRequestXPath("DRAFTCHED"),
            ChedSampleResource
        );

    [Fact]
    public async Task Get_WhenChedIsDraft_ReturnsNotFound()
    {
        StubDraftChed();

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("DRAFTCHED", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Find_AsksTracesForTheNonDraftStatusesOnly()
    {
        string? requestBody = null;
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    FindChedCertificateSoapAction,
                    "/*[local-name() = 'FindChedCertificateRequest']"
                )
            )
            .RespondWith(
                SoapUtilities.CaptureRequestAndRespond(
                    "Api.Tests.Samples.CHED.FindChedCertificateResponse.xml",
                    body => requestBody = body
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.FindChedUpdates(
            new DateTime(2002, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            0,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            ["1", "35", "41", "42", "44", "55", "64", "68", "70", "97", "99", "122", "124", "146"],
            SoapUtilities.StatusCodesInEnvelope(requestBody!)
        );
    }

    [Fact]
    public async Task Find_WhenOffsetIsLessThanZero_ReturnsBadRequest()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");

        var response = await client.FindChedUpdates(
            new DateTimeOffset(2002, 10, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            -1,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
        await Verify((response.Error as ValidationApiException)?.Content);
    }

    [Fact]
    public async Task Find_WhenValidRequest_AndNoOptionalParameters_ReturnsOk()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    FindChedCertificateSoapAction,
                    "/*[local-name() = 'FindChedCertificateRequest']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.CHED.FindChedCertificateResponse.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.FindChedUpdates(
            new DateTime(2002, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            0,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDCHEDSummaryProfile>(),
            response.ContentHeaders?.ContentType?.MediaType
        );
        await Verify(response.Content);
    }

    [Fact]
    public async Task Find_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    FindChedCertificateSoapAction,
                    "/*[local-name() = 'FindChedCertificateRequest']"
                        + "/*[local-name() = 'UpdateDateTimeRange']"
                        + "/*[local-name() = 'From' and contains(text(), '1999')]"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.FindChedUpdates(
            new DateTime(1999, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            0,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }
}
