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
    private const string GetCertificateAttachmentSoapAction = "\"getCertificateAttachment\"";

    private static string AttachmentRequestXPath(string chedId) =>
        $"/*[local-name() = 'GetCertificateAttachmentRequest']/*[local-name() = 'ChedCertificateReference' and text() = '{chedId}']";

    private static string AttachmentRequestXPath(string chedId, long documentId, string fileName) =>
        $"/*[local-name() = 'GetCertificateAttachmentRequest'"
        + $" and *[local-name() = 'ChedCertificateReference' and text() = '{chedId}']"
        + $" and *[local-name() = 'DocumentId' and text() = '{documentId}']"
        + $" and *[local-name() = 'FileName' and text() = '{fileName}']]";

    [Fact]
    public async Task Get_ReturnsMappedDefraUNVTDCHEDProfile()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text()]"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.CHED.GetChedResponse_CHEDA.XI.2026.0000063.xml"
                        )
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

    [Fact]
    public async Task GetAttachment_ReturnsTheFileWithItsContentTypeAndFileName()
    {
        var fileBytes = "%PDF-1.4 attachment"u8.ToArray();

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("CHEDA.XI.2026.0000063", 1001, "health-certificate.pdf")
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.OK,
                            $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              <s:Body>
                                <GetCertificateAttachmentResponse
                                    xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1"
                                    xmlns:xmime="http://www.w3.org/2005/05/xmlmime"
                                    fileName="health-certificate.pdf"
                                    xmime:contentType="application/pdf">
                                  <Attachment>{Convert.ToBase64String(fileBytes)}</Attachment>
                                </GetCertificateAttachmentResponse>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(
            "CHEDA.XI.2026.0000063",
            1001,
            "health-certificate.pdf",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("health-certificate.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(fileBytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("BADSOAP")
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
        var response = await client.GetChedCertificationAttachment(
            "BADSOAP",
            1,
            "file.pdf",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAttachment_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("COMMFAIL")
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
        var response = await client.GetChedCertificationAttachment(
            "COMMFAIL",
            1,
            "file.pdf",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("MISSING")
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
                                  <faultstring>Attachment not found</faultstring>
                                  <detail>
                                    <CertificateAttachmentNotFoundException xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1" />
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(
            "MISSING",
            42,
            "file.pdf",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await Verify(await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsPermissionDeniedFault_ReturnsForbidden()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("FORBIDDEN")
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
                                    <CertificateAttachmentPermissionDeniedException xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1" />
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(
            "FORBIDDEN",
            1,
            "file.pdf",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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
