using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.SQS.Model;
using Api.Attachments;
using Defra.TradeImports.CdpUploader;
using Defra.TradeImports.Messaging.Consuming;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Trade.Gateway.Api.Contract.Certificate;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

/// <summary>
/// GET certificates/cheds/{id}/attachments/{attachmentId} end to end: the request is recorded and
/// queued, the download consumer fetches the attachment from TRACES and hands it to the CDP uploader
/// (both stubbed in WireMock), the uploader's scan callback lands, and the file is then served.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class ChedAttachmentEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string GetChedCertificateSoapAction = "\"getChedCertificate\"";

    // CertificateAttachmentsServiceV1 is SOAP 1.2 with MTOM, so the action travels in the Content-Type header.
    private const string GetCertificateAttachmentSoapAction = "getCertificateAttachment";

    private const string ChedSampleResource = "Api.Tests.Samples.CHED.GetChedResponse_CHEDA.XI.2026.0000063.xml";

    // The last element of the sample's exchanged-document ReferenceSPSReferencedDocument, which
    // AttachmentBinaryObject must follow to keep the schema's element order.
    private const string SampleSupportingDocumentId = """<ns4:ID schemeAgencyID="GB">455645665566</ns4:ID>""";

    private const string TradeLineItemEnd = "</ns4:IncludedSPSTradeLineItem>";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string ChedRequestXPath(string chedId) =>
        $"/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = '{chedId}']";

    private static string AttachmentRequestXPath(string chedId) =>
        $"/*[local-name() = 'GetCertificateAttachmentRequest']/*[local-name() = 'ChedCertificateReference' and text() = '{chedId}']";

    private static string AttachmentRequestXPath(string chedId, long documentId, string fileName) =>
        $"/*[local-name() = 'GetCertificateAttachmentRequest'"
        + $" and *[local-name() = 'ChedCertificateReference' and text() = '{chedId}']"
        + $" and *[local-name() = 'DocumentId' and text() = '{documentId}']"
        + $" and *[local-name() = 'FileName' and text() = '{fileName}']]";

    private static string AttachmentBinaryObject(long documentId, string fileName) =>
        $"""<ns4:AttachmentBinaryObject uri="uri:documentid:{documentId}" filename="{fileName}" />""";

    private static string UploadId(string chedId, long attachmentId) => $"upload-{chedId}-{attachmentId}";

    [Fact]
    public async Task GetAttachment_FirstRequest_QueuesTheDownloadAndReturnsAccepted()
    {
        const string chedId = "CHEDA.XI.2026.0002001";

        var response = await GetAttachment(chedId, 2001);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("5", response.Headers.RetryAfter?.ToString());
        Assert.Equal($"/certificates/cheds/{chedId}/attachments/2001", response.Headers.Location?.OriginalString);
        var status = await response.Content.ReadFromJsonAsync<AttachmentDownloadStatus>(Ct);
        Assert.Equal("Requested", status?.Status);
        Assert.Equal(chedId, status?.CertificateId);
        Assert.Equal(2001, status?.AttachmentId);

        var published = Assert.Single(factory.Publisher.For(chedId, 2001));
        Assert.Equal(TradeGatewayWebApplicationFactory.AttachmentsTopicArn, published.TopicArn);
        Assert.Equal("2001", published.DuplicationId);
        Assert.Equal("2001", published.MessageGroupId);

        Assert.Equal(ChedAttachmentStatus.Requested, factory.Attachments.Find(chedId, 2001)?.Status);
    }

    [Fact]
    public async Task GetAttachment_WhileTheDownloadIsInProgress_DoesNotQueueItAgain()
    {
        const string chedId = "CHEDA.XI.2026.0002002";

        await GetAttachment(chedId, 2002);
        var second = await GetAttachment(chedId, 2002);

        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Single(factory.Publisher.For(chedId, 2002));
    }

    [Fact]
    public async Task GetAttachment_OnceDownloadedAndScanned_ReturnsTheFileWithItsContentTypeAndFileName()
    {
        const string chedId = "CHEDA.XI.2026.0002003";
        var fileBytes = "%PDF-1.4 attachment"u8.ToArray();

        StubChedWithAttachments(
            chedId,
            exchangedDocumentAttachments: AttachmentBinaryObject(1000, "other.pdf")
                + AttachmentBinaryObject(2003, "health-certificate.pdf")
        );
        StubAttachment(chedId, 2003, "health-certificate.pdf", fileBytes);
        StubUploader(chedId, 2003);

        Assert.Equal(HttpStatusCode.Accepted, (await GetAttachment(chedId, 2003)).StatusCode);
        Assert.Null(await RunConsumer(chedId, 2003));

        var scanning = factory.Attachments.Find(chedId, 2003)!;
        Assert.Equal(ChedAttachmentStatus.Scanning, scanning.Status);
        Assert.Equal(UploadId(chedId, 2003), scanning.UploadId);
        Assert.Equal("health-certificate.pdf", scanning.FileName);
        Assert.Equal("application/pdf", scanning.ContentType);

        var initiate = JsonDocument.Parse(InitiateRequestBody(chedId)).RootElement;
        Assert.Equal(TradeGatewayWebApplicationFactory.AttachmentsBucket, initiate.GetProperty("s3Bucket").GetString());
        Assert.Equal($"ched-attachments/{chedId}/2003", initiate.GetProperty("s3Path").GetString());
        Assert.Equal("http://localhost/attachments/scan-results", initiate.GetProperty("callback").GetString());
        Assert.Equal(scanning.CallbackToken, initiate.GetProperty("metadata").GetProperty("callbackToken").GetString());

        var upload = UploadRequest(chedId, 2003);
        Assert.Equal(fileBytes, upload.BodyAsBytes);
        Assert.Equal("health-certificate.pdf", upload.Headers!["x-filename"].Single());
        // Declared as its allowed type: the uploader checks mimeTypes against the declared type.
        Assert.StartsWith("application/pdf", upload.Headers["Content-Type"].Single());
        Assert.Equal(
            ["application/pdf", "image/jpeg", "image/png", "application/msword", "application/vnd.ms-excel"],
            initiate.GetProperty("mimeTypes").EnumerateArray().Select(m => m.GetString()!).ToArray()
        );

        var stillScanning = await GetAttachment(chedId, 2003);
        Assert.Equal(HttpStatusCode.Accepted, stillScanning.StatusCode);
        Assert.Equal("Scanning", (await stillScanning.Content.ReadFromJsonAsync<AttachmentDownloadStatus>(Ct))?.Status);

        Assert.Equal(HttpStatusCode.OK, (await ScanCallback(chedId, 2003, FileStatuses.Complete)).StatusCode);

        var response = await GetAttachment(chedId, 2003);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("health-certificate.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(fileBytes, await response.Content.ReadAsByteArrayAsync(Ct));
        Assert.Single(factory.Publisher.For(chedId, 2003));
    }

    [Fact]
    public async Task GetAttachment_WhenAttachmentIsOnATradeLineItem_RequestsItByItsFileName()
    {
        const string chedId = "CHEDA.XI.2026.0002004";
        var fileBytes = "%PDF-1.4 line item attachment"u8.ToArray();

        StubChedWithAttachments(chedId, tradeLineItemAttachments: AttachmentBinaryObject(2004, "lab-report.pdf"));
        StubAttachment(chedId, 2004, "lab-report.pdf", fileBytes);
        StubUploader(chedId, 2004);

        await GetAttachment(chedId, 2004);
        Assert.Null(await RunConsumer(chedId, 2004));
        await ScanCallback(chedId, 2004, FileStatuses.Complete);

        var response = await GetAttachment(chedId, 2004);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(fileBytes, await response.Content.ReadAsByteArrayAsync(Ct));
    }

    [Fact]
    public async Task GetAttachment_WhenCertificateHasNoMatchingAttachment_ReturnsNotFound()
    {
        const string chedId = "CHEDA.XI.2026.0002005";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(3000, "other.pdf"));

        await GetAttachment(chedId, 3001);
        Assert.Null(await RunConsumer(chedId, 3001));

        var response = await GetAttachment(chedId, 3001);
        await AssertProblem(
            response,
            HttpStatusCode.NotFound,
            AttachmentErrorCodes.AttachmentNotFound,
            $"Ched certificate attachment '{chedId} - 3001' was not found."
        );
        Assert.DoesNotContain(
            factory.WireMockServer.LogEntries,
            entry =>
                entry.RequestMessage?.Body is { } body
                && body.Contains("GetCertificateAttachmentRequest")
                && body.Contains(chedId)
        );
        Assert.Null(InitiateRequestBodyOrNull(chedId));
    }

    [Fact]
    public async Task GetAttachment_WhenCertificateIsNotFound_ReturnsNotFound()
    {
        const string chedId = "ATTACHMENT-CHED-MISSING";

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(GetChedCertificateSoapAction, ChedRequestXPath(chedId))
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.InternalServerError,
                            $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              <s:Body>
                                <s:Fault>
                                  <faultcode>s:Client</faultcode>
                                  <faultstring>Certificate not found</faultstring>
                                  <detail>
                                    <ChedCertificateNotFoundException xmlns="http://ec.europa.eu/tracesnt/certificate/ched/v2">
                                      <CertificateIdentifier>{chedId}</CertificateIdentifier>
                                    </ChedCertificateNotFoundException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        await GetAttachment(chedId, 1);
        Assert.Null(await RunConsumer(chedId, 1));

        await AssertProblem(
            await GetAttachment(chedId, 1),
            HttpStatusCode.NotFound,
            AttachmentErrorCodes.ChedNotFound,
            $"Ched certificate '{chedId}' was not found."
        );
    }

    [Fact]
    public async Task GetAttachment_WhenChedIsDraft_ReturnsNotFound()
    {
        const string chedId = "ATTACHMENT-DRAFTCHED";

        SoapUtilities.StubDraftCertificate(
            factory.WireMockServer,
            GetChedCertificateSoapAction,
            ChedRequestXPath(chedId),
            ChedSampleResource
        );

        await GetAttachment(chedId, 1);
        Assert.Null(await RunConsumer(chedId, 1));

        await AssertProblem(
            await GetAttachment(chedId, 1),
            HttpStatusCode.NotFound,
            AttachmentErrorCodes.ChedNotFound,
            $"Ched certificate '{chedId}' was not found."
        );
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        const string chedId = "ATTACHMENT-MISSING";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(42, "file.pdf"));
        StubAttachmentFault(chedId, "Attachment not found", "CertificateAttachmentNotFoundException");

        await GetAttachment(chedId, 42);
        Assert.Null(await RunConsumer(chedId, 42));

        await AssertProblem(
            await GetAttachment(chedId, 42),
            HttpStatusCode.NotFound,
            AttachmentErrorCodes.AttachmentNotFound,
            $"Ched certificate attachment '{chedId} - 42' was not found."
        );
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsPermissionDeniedFault_ReturnsForbidden()
    {
        const string chedId = "ATTACHMENT-FORBIDDEN";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(1, "file.pdf"));
        StubAttachmentFault(chedId, "Permission denied", "CertificateAttachmentPermissionDeniedException");

        await GetAttachment(chedId, 1);
        Assert.Null(await RunConsumer(chedId, 1));

        await AssertProblem(
            await GetAttachment(chedId, 1),
            HttpStatusCode.Forbidden,
            AttachmentErrorCodes.TracesPermissionDenied
        );
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        const string chedId = "ATTACHMENT-BADSOAP";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(1, "file.pdf"));
        StubAttachmentFault(chedId, "SAXException: invalid request");

        await GetAttachment(chedId, 1);
        // Not transient: settled on the record rather than redelivered.
        Assert.Null(await RunConsumer(chedId, 1));

        await AssertProblem(
            await GetAttachment(chedId, 1),
            HttpStatusCode.InternalServerError,
            AttachmentErrorCodes.TracesInvalidRequest
        );
    }

    [Fact]
    public async Task GetAttachment_WhenTracesCommunicationFails_ReturnsBadGateway_AndTheMessageIsRetried()
    {
        const string chedId = "ATTACHMENT-COMMFAIL";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(1, "file.pdf"));
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoap12MtomRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath(chedId)
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        await GetAttachment(chedId, 1);

        // Rethrown, so SQS makes the message visible again.
        Assert.NotNull(await RunConsumer(chedId, 1));

        var response = await GetAttachment(chedId, 1);
        await AssertProblem(
            response,
            HttpStatusCode.BadGateway,
            AttachmentErrorCodes.TracesUnavailable,
            "An error occurred communicating with TRACES."
        );
        Assert.DoesNotContain("upstream failed", await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal(AttachmentErrorSource.Traces, factory.Attachments.Find(chedId, 1)?.Error?.Source);
    }

    [Fact]
    public async Task GetAttachment_WhenTheUploaderFails_ReturnsBadGateway_AndTheMessageIsRetried()
    {
        const string chedId = "CHEDA.XI.2026.0002006";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(2006, "file.pdf"));
        StubAttachment(chedId, 2006, "file.pdf", "%PDF-1.4"u8.ToArray());
        factory
            .WireMockServer.Given(InitiateRequest(chedId))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.ServiceUnavailable).WithBody("down"));

        await GetAttachment(chedId, 2006);
        Assert.IsType<CdpUploaderException>(await RunConsumer(chedId, 2006));

        await AssertProblem(
            await GetAttachment(chedId, 2006),
            HttpStatusCode.BadGateway,
            AttachmentErrorCodes.UploadFailed
        );
        Assert.Equal(AttachmentErrorSource.Uploader, factory.Attachments.Find(chedId, 2006)?.Error?.Source);
    }

    [Fact]
    public async Task GetAttachment_WhenTheScanRejectsTheFile_ReturnsTheScanError()
    {
        const string chedId = "CHEDA.XI.2026.0002007";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(2007, "virus.pdf"));
        StubAttachment(chedId, 2007, "virus.pdf", "X5O!P%@AP"u8.ToArray());
        StubUploader(chedId, 2007);

        await GetAttachment(chedId, 2007);
        Assert.Null(await RunConsumer(chedId, 2007));
        Assert.Equal(
            HttpStatusCode.OK,
            (
                await ScanCallback(
                    chedId,
                    2007,
                    FileStatuses.Rejected,
                    errorCode: "FILE_VIRUS",
                    errorMessage: "The selected file contains a virus"
                )
            ).StatusCode
        );

        await AssertProblem(
            await GetAttachment(chedId, 2007),
            HttpStatusCode.UnprocessableEntity,
            AttachmentErrorCodes.FileVirus,
            "The selected file contains a virus"
        );
    }

    [Fact]
    public async Task GetAttachment_WhenTheFileTypeIsNotAllowed_ReturnsUnsupportedWithoutUploading()
    {
        const string chedId = "CHEDA.XI.2026.0002010";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(2010, "photo.gif"));
        StubAttachment(chedId, 2010, "photo.gif", "GIF89a"u8.ToArray(), contentType: "image/gif");

        await GetAttachment(chedId, 2010);
        // Not transient, so the message is settled rather than redelivered.
        Assert.Null(await RunConsumer(chedId, 2010));

        await AssertProblem(
            await GetAttachment(chedId, 2010),
            HttpStatusCode.UnprocessableEntity,
            AttachmentErrorCodes.UnsupportedFileTypeCode,
            "Attachments of type 'image/gif' are not supported. Supported types: application/pdf, image/jpeg, "
                + "image/png, application/msword, application/vnd.ms-excel."
        );
        Assert.Null(InitiateRequestBodyOrNull(chedId));
        Assert.Single(factory.Publisher.For(chedId, 2010));
    }

    [Fact]
    public async Task GetAttachment_WhenTracesGivesNoSpecificType_RecognisesAPdfByItsName()
    {
        const string chedId = "CHEDA.XI.2026.0002011";
        var fileBytes = "%PDF-1.4 untyped"u8.ToArray();

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(2011, "Report.PDF"));
        StubAttachment(chedId, 2011, "Report.PDF", fileBytes, contentType: "application/octet-stream");
        StubUploader(chedId, 2011);

        await GetAttachment(chedId, 2011);
        Assert.Null(await RunConsumer(chedId, 2011));
        Assert.StartsWith("application/pdf", UploadRequest(chedId, 2011).Headers!["Content-Type"].Single());
        await ScanCallback(chedId, 2011, FileStatuses.Complete);

        var response = await GetAttachment(chedId, 2011);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    // Legacy Office files are only detected as the generic compound file format.
    [InlineData(2013, "certificate.doc", "application/msword", "application/x-cfb")]
    [InlineData(2014, "consignment.xls", "application/vnd.ms-excel", "application/x-cfb")]
    [InlineData(2015, "photo.jpeg", "image/jpeg", "image/jpeg")]
    [InlineData(2016, "label.png", "image/png", "image/png")]
    public async Task GetAttachment_ServesEachSupportedType(
        long attachmentId,
        string fileName,
        string contentType,
        string detectedContentType
    )
    {
        var chedId = $"CHEDA.XI.2026.000{attachmentId}";
        byte[] fileBytes = [0x01, 0x02, 0x03];

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(attachmentId, fileName));
        StubAttachment(chedId, attachmentId, fileName, fileBytes, contentType: contentType);
        StubUploader(chedId, attachmentId);

        await GetAttachment(chedId, attachmentId);
        Assert.Null(await RunConsumer(chedId, attachmentId));
        Assert.StartsWith(contentType, UploadRequest(chedId, attachmentId).Headers!["Content-Type"].Single());
        await ScanCallback(chedId, attachmentId, FileStatuses.Complete, detectedContentType: detectedContentType);

        var response = await GetAttachment(chedId, attachmentId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(fileName, response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(fileBytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAttachment_WhenTheScannerFindsTheContentIsNotAPdf_ReturnsInvalidType()
    {
        const string chedId = "CHEDA.XI.2026.0002012";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(2012, "fake.pdf"));
        StubAttachment(chedId, 2012, "fake.pdf", [0x89, 0x50, 0x4E, 0x47]);
        StubUploader(chedId, 2012);

        await GetAttachment(chedId, 2012);
        Assert.Null(await RunConsumer(chedId, 2012));
        Assert.Equal(
            HttpStatusCode.OK,
            (await ScanCallback(chedId, 2012, FileStatuses.Complete, detectedContentType: "image/png")).StatusCode
        );

        await AssertProblem(
            await GetAttachment(chedId, 2012),
            HttpStatusCode.UnprocessableEntity,
            AttachmentErrorCodes.FileInvalidType,
            // PNG is allowed, but not when it was declared as a PDF.
            "The attachment's content does not match its file type."
        );
    }

    [Fact]
    public async Task ScanCallback_WithTheWrongToken_IsRejectedAndChangesNothing()
    {
        const string chedId = "CHEDA.XI.2026.0002008";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(2008, "file.pdf"));
        StubAttachment(chedId, 2008, "file.pdf", "%PDF-1.4"u8.ToArray());
        StubUploader(chedId, 2008);
        await GetAttachment(chedId, 2008);
        await RunConsumer(chedId, 2008);

        var response = await ScanCallback(chedId, 2008, FileStatuses.Complete, callbackToken: "not-the-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ChedAttachmentStatus.Scanning, factory.Attachments.Find(chedId, 2008)?.Status);
    }

    [Fact]
    public async Task ScanCallback_IsAnonymous()
    {
        var response = await factory
            .CreateClient()
            .PostAsJsonAsync("/attachments/scan-results", new { uploadStatus = "ready", metadata = new { } }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAttachment_WhenPublishingFails_ReturnsAnErrorAndForgetsTheRequest()
    {
        const string chedId = "CHEDA.XI.2026.0002009";
        factory.Publisher.FailWhen = body =>
            body.Contains(chedId) ? new InvalidOperationException("SNS unavailable") : null;

        try
        {
            var response = await GetAttachment(chedId, 2009);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Null(factory.Attachments.Find(chedId, 2009));
        }
        finally
        {
            factory.Publisher.FailWhen = _ => null;
        }
    }

    // --- helpers ---------------------------------------------------------------------------------

    private async Task<HttpResponseMessage> GetAttachment(string chedId, long attachmentId)
    {
        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        return await client.GetChedCertificationAttachment(chedId, attachmentId, Ct);
    }

    /// <summary>Delivers the last queued message for the attachment, as the SQS consumer would.</summary>
    private async Task<Exception?> RunConsumer(string chedId, long attachmentId)
    {
        var message = factory.Publisher.For(chedId, attachmentId)[^1];

        await using var scope = factory.Services.CreateAsyncScope();

        // Done by TracingConsumeMiddleware in the running service; the uploader client propagates it.
        scope.ServiceProvider.GetRequiredService<HeaderPropagationValues>().Headers = new Dictionary<
            string,
            StringValues
        >(StringComparer.OrdinalIgnoreCase);

        var consumer = scope.ServiceProvider.GetRequiredService<ChedAttachmentDownloadConsumer>();
        try
        {
            await consumer.ConsumeAsync(
                new MessageContext
                {
                    Message = new Message { Body = message.Body, MessageId = Guid.NewGuid().ToString() },
                    QueueUrl = "test",
                    ConsumerType = typeof(ChedAttachmentDownloadConsumer),
                },
                Ct
            );
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>Plays the CDP uploader's scan callback, moving the uploaded bytes to the store on success.</summary>
    private async Task<HttpResponseMessage> ScanCallback(
        string chedId,
        long attachmentId,
        string fileStatus,
        string? errorCode = null,
        string? errorMessage = null,
        string? callbackToken = null,
        string? detectedContentType = "application/pdf"
    )
    {
        var record = factory.Attachments.Find(chedId, attachmentId)!;
        var s3Key = $"ched-attachments/{chedId}/{attachmentId}/{record.UploadId}/file-1";

        if (fileStatus == FileStatuses.Complete)
        {
            factory.AttachmentStore.Put(
                TradeGatewayWebApplicationFactory.AttachmentsBucket,
                s3Key,
                UploadRequest(chedId, attachmentId).BodyAsBytes!
            );
        }

        var rejected = fileStatus == FileStatuses.Rejected;
        var payload = new
        {
            uploadStatus = "ready",
            metadata = new
            {
                chedId,
                attachmentId = attachmentId.ToString(),
                callbackToken = callbackToken ?? record.CallbackToken,
            },
            form = new
            {
                file = new
                {
                    fileId = "file-1",
                    filename = record.FileName,
                    contentType = record.ContentType,
                    detectedContentType,
                    fileStatus,
                    contentLength = record.ContentLength,
                    s3Key = rejected ? null : s3Key,
                    s3Bucket = rejected ? null : TradeGatewayWebApplicationFactory.AttachmentsBucket,
                    hasError = rejected,
                    errorCode,
                    errorMessage,
                },
            },
            numberOfRejectedFiles = rejected ? 1 : 0,
        };

        return await factory.CreateClient().PostAsJsonAsync("/attachments/scan-results", payload, Ct);
    }

    private static async Task AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string errorCode,
        string? detail = null
    )
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal(errorCode, problem.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal((int)statusCode, problem.RootElement.GetProperty("status").GetInt32());
        if (detail is not null)
            Assert.Equal(detail, problem.RootElement.GetProperty("detail").GetString());
    }

    private static IRequestBuilder InitiateRequest(string chedId) =>
        Request
            .Create()
            .WithPath("/initiate")
            .UsingPost()
            .WithBody(new JsonPartialMatcher(new { metadata = new { chedId } }));

    private void StubUploader(string chedId, long attachmentId)
    {
        var uploadId = UploadId(chedId, attachmentId);

        factory
            .WireMockServer.Given(InitiateRequest(chedId))
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.Created)
                    .WithBodyAsJson(
                        new
                        {
                            uploadId,
                            // Absolute, as the uploader answers in development: only the path is used.
                            uploadUrl = $"http://cdp-uploader:7337/upload-and-scan/{uploadId}",
                            statusUrl = $"http://cdp-uploader:7337/status/{uploadId}",
                        }
                    )
            );

        factory
            .WireMockServer.Given(Request.Create().WithPath($"/upload-and-scan/{uploadId}").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Redirect).WithHeader("Location", "/"));
    }

    private string? InitiateRequestBodyOrNull(string chedId) =>
        factory
            .WireMockServer.LogEntries.Select(entry => entry.RequestMessage)
            .LastOrDefault(request => request?.Path == "/initiate" && request.Body?.Contains(chedId) == true)
            ?.Body;

    private string InitiateRequestBody(string chedId) => InitiateRequestBodyOrNull(chedId)!;

    private WireMock.IRequestMessage UploadRequest(string chedId, long attachmentId) =>
        factory
            .WireMockServer.LogEntries.Select(entry => entry.RequestMessage)
            .Last(request => request?.Path == $"/upload-and-scan/{UploadId(chedId, attachmentId)}")!;

    /// <summary>
    /// Stubs getChedCertificate for <paramref name="chedId"/> with the sample CHED, adding the given
    /// AttachmentBinaryObject elements to the exchanged document and/or every trade line item.
    /// </summary>
    private void StubChedWithAttachments(
        string chedId,
        string exchangedDocumentAttachments = "",
        string tradeLineItemAttachments = ""
    )
    {
        var tradeLineItemDocument =
            tradeLineItemAttachments.Length == 0
                ? ""
                : $"<ns4:ReferenceSPSReferencedDocument>{tradeLineItemAttachments}</ns4:ReferenceSPSReferencedDocument>";

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(GetChedCertificateSoapAction, ChedRequestXPath(chedId))
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            ChedSampleResource,
                            new TokenSubstitution
                            {
                                Token = SampleSupportingDocumentId,
                                Substitution = SampleSupportingDocumentId + exchangedDocumentAttachments,
                            },
                            new TokenSubstitution
                            {
                                Token = TradeLineItemEnd,
                                Substitution = tradeLineItemDocument + TradeLineItemEnd,
                            }
                        )
                    )
            );
    }

    private void StubAttachment(
        string chedId,
        long documentId,
        string fileName,
        byte[] fileBytes,
        string contentType = "application/pdf"
    ) =>
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoap12MtomRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath(chedId, documentId, fileName)
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubSoap12MtomResponseMessage(
                            HttpStatusCode.OK,
                            $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <env:Envelope xmlns:env="http://www.w3.org/2003/05/soap-envelope">
                              <env:Body>
                                <GetCertificateAttachmentResponse
                                    xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1"
                                    xmlns:xmime="http://www.w3.org/2005/05/xmlmime"
                                    fileName="{fileName}"
                                    xmime:contentType="{contentType}">
                                  <Attachment><xop:Include xmlns:xop="http://www.w3.org/2004/08/xop/include" href="cid:attachment@traces" /></Attachment>
                                </GetCertificateAttachmentResponse>
                              </env:Body>
                            </env:Envelope>
                            """,
                            "attachment@traces",
                            fileBytes
                        )
                    )
            );

    private void StubAttachmentFault(string chedId, string reason, string? detailElement = null)
    {
        var detail = detailElement is null
            ? ""
            : $"""
                <env:Detail>
                  <{detailElement} xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1" />
                </env:Detail>
                """;

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoap12MtomRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath(chedId)
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubSoap12ResponseMessage(
                            HttpStatusCode.InternalServerError,
                            $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <env:Envelope xmlns:env="http://www.w3.org/2003/05/soap-envelope">
                              <env:Body>
                                <env:Fault>
                                  <env:Code><env:Value>env:Sender</env:Value></env:Code>
                                  <env:Reason><env:Text xml:lang="en">{reason}</env:Text></env:Reason>
                                  {detail}
                                </env:Fault>
                              </env:Body>
                            </env:Envelope>
                            """
                        )
                    )
            );
    }
}
