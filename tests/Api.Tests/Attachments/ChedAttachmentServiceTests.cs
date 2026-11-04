using Api.Attachments;
using Defra.TradeImports.Messaging.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

    private ChedAttachmentService CreateService() =>
        new(
            _repository,
            _store,
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
        Assert.Equal("99", published.DuplicationId);

        var record = _repository.Find(ChedId, AttachmentId)!;
        Assert.Equal(ChedAttachmentStatus.Requested, record.Status);
        Assert.Equal(Now.UtcDateTime, record.UpdatedAt);
        // A re-queue never extends the record's life, so it still expires before its S3 object.
        Assert.Equal((Now - TimeSpan.FromMinutes(16)).UtcDateTime, record.CreatedAt);
    }

    [Fact]
    public async Task RetryableFailure_Recent_ReturnsTheFailure()
    {
        Seed(ChedAttachmentStatus.Failed, TimeSpan.FromMinutes(1), Error(AttachmentErrorCodes.TracesUnavailable, true));

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        var failed = Assert.IsType<ChedAttachmentLookup.Failed>(result);
        Assert.Equal(AttachmentErrorCodes.TracesUnavailable, failed.Error.Code);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task RetryableFailure_Stale_IsRequeued()
    {
        Seed(ChedAttachmentStatus.Failed, TimeSpan.FromMinutes(30), Error(AttachmentErrorCodes.ChedNotFound, true));

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Pending>(result);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task ScanRejection_IsNeverRetried()
    {
        Seed(ChedAttachmentStatus.Failed, TimeSpan.FromDays(30), Error(AttachmentErrorCodes.FileVirus, false));

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Failed>(result);
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
            AttachmentErrorCodes.UnsupportedFileType(contentType, fileName, ["application/pdf"], Now.UtcDateTime)
        );
        record.ContentType = contentType;
        record.FileName = fileName;
        _repository.Save(record);
    }

    [Fact]
    public async Task Available_ButTheObjectIsGone_IsRequeued()
    {
        var record = Seed(ChedAttachmentStatus.Available, TimeSpan.FromDays(1));
        record.S3Bucket = "bucket";
        record.S3Key = "missing";
        _repository.Save(record);

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        Assert.IsType<ChedAttachmentLookup.Pending>(result);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Available_ReturnsTheStoredFile()
    {
        var record = Seed(ChedAttachmentStatus.Available, TimeSpan.FromDays(1));
        record.S3Bucket = "bucket";
        record.S3Key = "key";
        _repository.Save(record);
        _store.Put("bucket", "key", [1, 2, 3]);

        var result = await CreateService().GetOrRequest(ChedId, AttachmentId, Ct);

        var ready = Assert.IsType<ChedAttachmentLookup.Ready>(result);
        await using (ready.File)
        {
            Assert.Equal(3, ready.File.ContentLength);
        }
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
    }

    private AttachmentError Error(string code, bool retryable) =>
        new()
        {
            Source = AttachmentErrorSource.Traces,
            Code = code,
            Message = code,
            Retryable = retryable,
            OccurredAt = Now.UtcDateTime,
        };
}

public class AttachmentErrorCodesTests
{
    [Theory]
    [InlineData(AttachmentErrorCodes.ChedNotFound, 404)]
    [InlineData(AttachmentErrorCodes.AttachmentNotFound, 404)]
    [InlineData(AttachmentErrorCodes.TracesPermissionDenied, 403)]
    [InlineData(AttachmentErrorCodes.FileVirus, 422)]
    [InlineData(AttachmentErrorCodes.FileEmpty, 422)]
    [InlineData(AttachmentErrorCodes.FileTooLarge, 422)]
    [InlineData(AttachmentErrorCodes.FileInvalidType, 422)]
    [InlineData(AttachmentErrorCodes.TracesInvalidRequest, 500)]
    [InlineData(AttachmentErrorCodes.UnexpectedError, 500)]
    [InlineData(AttachmentErrorCodes.TracesUnavailable, 502)]
    [InlineData(AttachmentErrorCodes.UploadFailed, 502)]
    [InlineData(AttachmentErrorCodes.FileUploadFailed, 502)]
    [InlineData("SOMETHING_NEW", 502)]
    public void ToStatusCode_MapsEachCode(string code, int statusCode) =>
        Assert.Equal(statusCode, AttachmentErrorCodes.ToStatusCode(code));

    [Theory]
    [InlineData("FILE_VIRUS", false)]
    [InlineData("FILE_EMPTY", false)]
    [InlineData("FILE_UPLOAD_FAILED", true)]
    [InlineData("FILE_DOWNLOAD_FAILED", true)]
    [InlineData(null, true)]
    public void ScanRejected_OnlyRetriesTheUploadersOwnFailures(string? code, bool retryable)
    {
        var error = AttachmentErrorCodes.ScanRejected(code, null, DateTime.UtcNow);

        Assert.Equal(retryable, error.Retryable);
        Assert.Equal(AttachmentErrorSource.Scan, error.Source);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
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

    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("APPLICATION/PDF", true)]
    [InlineData("image/png", false)]
    [InlineData("application/octet-stream", false)]
    [InlineData(null, false)]
    public void IsAllowed_ChecksTheDetectedType(string? mimeType, bool expected) =>
        Assert.Equal(expected, AttachmentFileTypes.IsAllowed(PdfOnly, mimeType));
}
