using System.Net;
using System.ServiceModel;
using System.ServiceModel.Security;
using Amazon.S3;
using Api.Attachments;
using Defra.TradeImports.CdpUploader;
using Defra.TradeImports.Messaging.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TracesNT.Exceptions;

namespace Api.Tests.Attachments;

public class ChedAttachmentServiceTests
{
    private const string ChedId = "CHEDA.GB.2026.0000001";
    private const long AttachmentId = 99;
    private const string TopicArn = "arn:aws:sns:eu-west-2:000000000000:attachments.fifo";

    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryChedAttachmentRepository _repository = new();
    private readonly InMemoryAttachmentStore _store = new();
    private readonly RecordingSnsPublisher _publisher = new();
    private readonly FixedTimeProvider _time = new(Now);

    private static readonly AllowedFileType Pdf = new() { MimeType = "application/pdf", Extensions = [".pdf"] };

    private List<AllowedFileType> _allowed = [Pdf];

    private IAttachmentStore? _storeOverride;

    private ChedAttachmentService CreateService() =>
        new(
            _repository,
            _storeOverride ?? _store,
            new TopicPublisher<ChedAttachmentDownloadRequested>(_publisher, TopicArn),
            Options.Create(
                new ChedAttachmentOptions
                {
                    TopicArn = TopicArn,
                    QueueUrl = "queue",
                    S3Bucket = "bucket",
                    RetryAfter = TimeSpan.FromMinutes(15),
                    AllowedFileTypes = _allowed,
                }
            ),
            _time,
            NullLogger<ChedAttachmentService>.Instance
        );

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ChedAttachmentEntity Seed(ChedAttachmentStatus status, TimeSpan age, AttachmentError? error = null)
    {
        var record = ChedAttachmentEntity.NewRequest(ChedId, AttachmentId, (Now - age).UtcDateTime);
        record.Status = status;
        record.Error = error;
        record.Requests = 1;
        _repository.Save(record);
        return record;
    }

    [Theory]
    [InlineData(ChedAttachmentStatus.Requested)]
    [InlineData(ChedAttachmentStatus.Downloading)]
    [InlineData(ChedAttachmentStatus.Scanning)]
    public async Task InProgress_RecentlyUpdated_IsPendingWithoutRequeue(ChedAttachmentStatus status)
    {
        Seed(status, TimeSpan.FromMinutes(14));

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Pending>(result);
        Assert.Empty(_publisher.Published);
        Assert.Equal(status, _repository.Find(ChedId, AttachmentId)?.Status);
    }

    [Theory]
    [InlineData(ChedAttachmentStatus.Requested)]
    [InlineData(ChedAttachmentStatus.Downloading)]
    [InlineData(ChedAttachmentStatus.Scanning)]
    public async Task InProgress_Stale_IsRequeued(ChedAttachmentStatus status)
    {
        Seed(status, TimeSpan.FromMinutes(16));

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Pending>(result);
        var published = Assert.Single(_publisher.Published);
        Assert.Equal(TopicArn, published.TopicArn);
        Assert.Equal("99-2", published.DuplicationId);

        var record = _repository.Find(ChedId, AttachmentId)!;
        Assert.Equal(ChedAttachmentStatus.Requested, record.Status);
        Assert.Equal(Now.UtcDateTime, record.UpdatedAt);
        // A re-queue never extends the record's life, so it still expires before its S3 object.
        Assert.Equal((Now - TimeSpan.FromMinutes(16)).UtcDateTime, record.CreatedAt);
    }

    [Theory]
    [InlineData(AttachmentFailureCodes.Other)]
    [InlineData(AttachmentFailureCodes.TracesUnavailable)]
    [InlineData(AttachmentFailureCodes.TracesTimeout)]
    [InlineData(AttachmentFailureCodes.TracesRateLimited)]
    [InlineData(AttachmentFailureCodes.UploaderFailure)]
    [InlineData(AttachmentFailureCodes.StorageFailure)]
    // Recorded before the DDF codes: counts as any other reason.
    [InlineData("TRACES_UNAVAILABLE")]
    public async Task RetriableFailure_IsRequeuedStraightAway(string code)
    {
        Seed(ChedAttachmentStatus.Failed, TimeSpan.FromSeconds(10), Error(code));

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Pending>(result);
        var published = Assert.Single(_publisher.Published);
        // A new de-duplication id, so SNS does not drop it as a repeat of the failed attempt.
        Assert.Equal($"{AttachmentId}-2", published.DuplicationId);
        Assert.Equal(ChedAttachmentStatus.Requested, _repository.Find(ChedId, AttachmentId)?.Status);
    }

    [Theory]
    [InlineData(AttachmentFailureCodes.InvalidCredentials)]
    [InlineData(AttachmentFailureCodes.PermissionDenied)]
    [InlineData(AttachmentFailureCodes.MalwareDetected)]
    [InlineData(AttachmentFailureCodes.UnsupportedFileType)]
    [InlineData(AttachmentFailureCodes.FileTooLarge)]
    [InlineData(AttachmentFailureCodes.NotFound)]
    public async Task NonRetriableFailure_IsReturnedWithoutRetrying(string code)
    {
        Seed(ChedAttachmentStatus.Failed, TimeSpan.FromDays(20), Error(code));

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        var failed = Assert.IsType<ChedAttachmentLookup.Failed>(result);
        Assert.Equal(code, failed.Error.Code);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task UnsupportedType_WhileStillNotAllowed_ReturnsTheFailure()
    {
        SeedUnsupported("image/jpeg", "photo.jpg");

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Failed>(result);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task UnsupportedType_OnceTheListAllowsIt_IsRequeuedStraightAway()
    {
        SeedUnsupported("image/jpeg", "photo.jpg");
        _allowed = [Pdf, new AllowedFileType { MimeType = "image/jpeg", Extensions = [".jpg"] }];

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Pending>(result);
        Assert.Single(_publisher.Published);
    }

    private void SeedUnsupported(string contentType, string fileName)
    {
        var record = Seed(
            ChedAttachmentStatus.Failed,
            TimeSpan.FromMinutes(1),
            AttachmentFailureCodes.Unsupported(contentType, fileName, ["application/pdf"], Now.UtcDateTime)
        );
        record.ContentType = contentType;
        record.FileName = fileName;
        _repository.Save(record);
    }

    private void SeedAvailable()
    {
        var record = Seed(ChedAttachmentStatus.Available, TimeSpan.FromDays(1));
        record.S3Bucket = "bucket";
        record.S3Key = "key";
        _repository.Save(record);
    }

    [Fact]
    public async Task Available_IsReportedWithoutReadingTheBucket()
    {
        SeedAvailable();
        _storeOverride = Substitute.For<IAttachmentStore>();

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Available>(result);
        await _storeOverride.DidNotReceiveWithAnyArgs().Open(default!, default!, Ct);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Open_Available_ReturnsTheStoredFile()
    {
        SeedAvailable();
        _store.Put("bucket", "key", [1, 2, 3]);

        var result = await CreateService().OpenOrRequest(ChedId, AttachmentId, Ct);

        var ready = Assert.IsType<ChedAttachmentLookup.Ready>(result);
        await using (ready.File)
        {
            Assert.Equal(3, ready.File.ContentLength);
        }
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Open_Available_ButTheObjectIsGone_IsRequeued()
    {
        SeedAvailable();

        var result = await CreateService().OpenOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Pending>(result);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Open_WhenTheBucketCannotBeRead_ReturnsAStorageFailureWithoutChangingTheRecord()
    {
        SeedAvailable();
        _storeOverride = Substitute.For<IAttachmentStore>();
        _storeOverride
            .Open(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("slow down"));

        var result = await CreateService().OpenOrRequest(ChedId, AttachmentId, Ct);

        var failed = Assert.IsType<ChedAttachmentLookup.Failed>(result);
        Assert.Equal(AttachmentFailureCodes.StorageFailure, failed.Error.Code);
        Assert.Equal(ChedAttachmentStatus.Available, _repository.Find(ChedId, AttachmentId)?.Status);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Requeue_WhenPublishingFails_RestoresTheRecord()
    {
        Seed(ChedAttachmentStatus.Scanning, TimeSpan.FromHours(1));
        _publisher.FailWhen = _ => new InvalidOperationException("SNS down");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().GetOrRequest(ChedId, AttachmentId, Ct)
        );

        var record = _repository.Find(ChedId, AttachmentId)!;
        Assert.Equal(ChedAttachmentStatus.Scanning, record.Status);
        Assert.Equal((Now - TimeSpan.FromHours(1)).UtcDateTime, record.UpdatedAt);
        Assert.Equal(1, record.Requests);
    }

    private static AttachmentError Error(string code) =>
        new()
        {
            Source = AttachmentErrorSource.Traces,
            Code = code,
            Message = code,
            Retryable = AttachmentFailureCodes.IsRetriable(code),
            OccurredAt = Now.UtcDateTime,
        };
}

public class AttachmentFailureCodesTests
{
    [Theory]
    [InlineData(AttachmentFailureCodes.Other, "Any other reason", true, 500)]
    [InlineData(AttachmentFailureCodes.TracesUnavailable, "TRACES service unavailable", true, 502)]
    [InlineData(AttachmentFailureCodes.TracesTimeout, "Document retrieval timeout", true, 504)]
    [InlineData(AttachmentFailureCodes.TracesRateLimited, "TRACES request rate limited", true, 502)]
    [InlineData(AttachmentFailureCodes.UploaderFailure, "CDP uploader failure", true, 502)]
    [InlineData(AttachmentFailureCodes.StorageFailure, "S3 storage failure", true, 502)]
    [InlineData(AttachmentFailureCodes.InvalidCredentials, "Invalid credentials", false, 502)]
    [InlineData(AttachmentFailureCodes.PermissionDenied, "Permission denied", false, 403)]
    [InlineData(AttachmentFailureCodes.MalwareDetected, "Malware detected", false, 422)]
    [InlineData(AttachmentFailureCodes.UnsupportedFileType, "Unsupported file type", false, 422)]
    [InlineData(AttachmentFailureCodes.FileTooLarge, "File exceeds maximum size", false, 422)]
    [InlineData(AttachmentFailureCodes.NotFound, "Document not found or no longer available", false, 404)]
    [InlineData("SOMETHING_OLD", "Any other reason", true, 500)]
    public void EachCode_HasItsReasonRetriabilityAndStatus(string code, string reason, bool retriable, int status)
    {
        Assert.Equal(reason, AttachmentFailureCodes.ReasonFor(code));
        Assert.Equal(retriable, AttachmentFailureCodes.IsRetriable(code));
        Assert.Equal(status, AttachmentFailureCodes.ToStatusCode(code));
    }

    [Fact]
    public void TheTableHasTwelveCodes() => Assert.Equal(12, AttachmentFailureCodes.All.Count);

    public static TheoryData<Exception, string, AttachmentErrorSource> Exceptions =>
        new()
        {
            {
                new PermissionDeniedException("x", new Exception()),
                AttachmentFailureCodes.PermissionDenied,
                AttachmentErrorSource.Traces
            },
            {
                Traces(new TimeoutException("timed out")),
                AttachmentFailureCodes.TracesTimeout,
                AttachmentErrorSource.Traces
            },
            {
                Traces(
                    new ProtocolException("The remote server returned an unexpected response: (429) Too Many Requests.")
                ),
                AttachmentFailureCodes.TracesRateLimited,
                AttachmentErrorSource.Traces
            },
            {
                Traces(new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests)),
                AttachmentFailureCodes.TracesRateLimited,
                AttachmentErrorSource.Traces
            },
            {
                Traces(new MessageSecurityException("The HTTP request is unauthorized")),
                AttachmentFailureCodes.InvalidCredentials,
                AttachmentErrorSource.Traces
            },
            {
                new InvalidSoapException(
                    "fault",
                    new FaultException(new FaultReason("bad token"), new FaultCode("FailedAuthentication"), null)
                ),
                AttachmentFailureCodes.InvalidCredentials,
                AttachmentErrorSource.Traces
            },
            {
                Traces(new CommunicationException("(502) Bad Gateway")),
                AttachmentFailureCodes.TracesUnavailable,
                AttachmentErrorSource.Traces
            },
            {
                new InvalidSoapException("fault", new FaultException("SAXException")),
                AttachmentFailureCodes.Other,
                AttachmentErrorSource.Traces
            },
            {
                new CdpUploaderException("down", HttpStatusCode.ServiceUnavailable),
                AttachmentFailureCodes.UploaderFailure,
                AttachmentErrorSource.Uploader
            },
            {
                new CdpUploaderException("too big", HttpStatusCode.RequestEntityTooLarge),
                AttachmentFailureCodes.FileTooLarge,
                AttachmentErrorSource.Uploader
            },
            {
                new AmazonS3Exception("slow down"),
                AttachmentFailureCodes.StorageFailure,
                AttachmentErrorSource.Storage
            },
            { new InvalidOperationException("bug"), AttachmentFailureCodes.Other, AttachmentErrorSource.Internal },
        };

    private static TracesCommunicationException Traces(Exception inner) => new("TRACES call failed", inner);

    [Theory]
    [MemberData(nameof(Exceptions))]
    public void FromException_ClassifiesTheFailure(Exception exception, string code, AttachmentErrorSource source)
    {
        var error = AttachmentFailureCodes.FromException(exception, DateTime.UtcNow);

        Assert.Equal(code, error.Code);
        Assert.Equal(source, error.Source);
        Assert.Equal(AttachmentFailureCodes.ReasonFor(code), error.Message);
        Assert.Equal(AttachmentFailureCodes.IsRetriable(code), error.Retryable);
        Assert.StartsWith(exception.GetType().Name, error.Detail);
    }

    [Theory]
    [InlineData("FILE_VIRUS", AttachmentFailureCodes.MalwareDetected)]
    [InlineData("FILE_TOO_LARGE", AttachmentFailureCodes.FileTooLarge)]
    [InlineData("FILE_INVALID_TYPE", AttachmentFailureCodes.UnsupportedFileType)]
    [InlineData("FILE_UPLOAD_FAILED", AttachmentFailureCodes.StorageFailure)]
    [InlineData("FILE_EMPTY", AttachmentFailureCodes.Other)]
    [InlineData("FILE_DOWNLOAD_FAILED", AttachmentFailureCodes.UploaderFailure)]
    [InlineData(null, AttachmentFailureCodes.UploaderFailure)]
    public void ScanRejected_MapsTheUploadersCode(string? uploaderCode, string code)
    {
        var error = AttachmentFailureCodes.ScanRejected(uploaderCode, "uploader says", DateTime.UtcNow);

        Assert.Equal(code, error.Code);
        Assert.Equal(AttachmentFailureCodes.ReasonFor(code), error.Message);
        Assert.Contains("uploader says", error.Detail);
    }
}

public class AttachmentFileTypesTests
{
    private static readonly List<AllowedFileType> PdfOnly =
    [
        new() { MimeType = "application/pdf", Extensions = [".pdf"] },
    ];

    [Theory]
    [InlineData("application/pdf", "a.pdf", "application/pdf")]
    [InlineData("Application/PDF; charset=binary", "a.pdf", "application/pdf")]
    [InlineData("application/pdf", "no-extension", "application/pdf")]
    [InlineData("application/octet-stream", "report.PDF", "application/pdf")]
    [InlineData(null, "report.pdf", "application/pdf")]
    [InlineData("", "report.pdf", "application/pdf")]
    [InlineData("image/jpeg", "photo.jpg", null)]
    // A specific type from TRACES wins over a misleading name.
    [InlineData("image/jpeg", "photo.pdf", null)]
    [InlineData("application/octet-stream", "data.bin", null)]
    [InlineData(null, "no-extension", null)]
    [InlineData(null, null, null)]
    public void Resolve_PdfOnly(string? contentType, string? fileName, string? expected) =>
        Assert.Equal(expected, AttachmentFileTypes.Resolve(PdfOnly, contentType, fileName));

    [Fact]
    public void Resolve_ExtensionsMayOmitTheDot()
    {
        List<AllowedFileType> allowed = [new() { MimeType = "image/png", Extensions = ["PNG"] }];

        Assert.Equal("image/png", AttachmentFileTypes.Resolve(allowed, null, "scan.png"));
    }

    private static readonly List<AllowedFileType> PdfPngAndWord =
    [
        new() { MimeType = "application/pdf", Extensions = [".pdf"] },
        new() { MimeType = "image/png", Extensions = [".png"] },
        new()
        {
            MimeType = "application/msword",
            Extensions = [".doc"],
            DetectedTypes = ["application/x-cfb"],
        },
    ];

    [Theory]
    [InlineData("application/pdf", "application/pdf", true)]
    [InlineData("application/pdf", "APPLICATION/PDF", true)]
    [InlineData("application/msword", "application/x-cfb", true)]
    [InlineData("application/msword", "application/msword", true)]
    // Allowed types, but not the one the file was declared as.
    [InlineData("application/pdf", "image/png", false)]
    [InlineData("application/pdf", "application/x-cfb", false)]
    [InlineData("image/png", "application/x-cfb", false)]
    [InlineData("application/pdf", "application/octet-stream", false)]
    // Declared as a type that is no longer on the list.
    [InlineData("image/jpeg", "image/jpeg", false)]
    [InlineData("application/pdf", null, false)]
    [InlineData(null, "application/pdf", false)]
    public void MatchesContent_ChecksTheDetectedTypeAgainstTheDeclaredOne(
        string? declared,
        string? detected,
        bool expected
    ) => Assert.Equal(expected, AttachmentFileTypes.MatchesContent(PdfPngAndWord, declared, detected));
}
