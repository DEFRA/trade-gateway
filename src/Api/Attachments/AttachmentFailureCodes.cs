using System.Net;
using System.ServiceModel;
using System.ServiceModel.Security;
using Amazon.S3;
using Defra.TradeImports.CdpUploader;
using TracesNT.Exceptions;

namespace Api.Attachments;

/// <summary>
/// The failure codes stored on a <see cref="ChedAttachmentEntity"/> and returned to callers. A
/// retriable failure is downloaded again the next time the attachment is requested; any other is
/// returned as it is until the record expires.
/// </summary>
public static class AttachmentFailureCodes
{
    public const string Other = "DDF_99";
    public const string TracesUnavailable = "DDF_01";
    public const string TracesTimeout = "DDF_02";
    public const string TracesRateLimited = "DDF_03";
    public const string UploaderFailure = "DDF_04";
    public const string StorageFailure = "DDF_05";
    public const string InvalidCredentials = "DDF_06";
    public const string PermissionDenied = "DDF_07";
    public const string MalwareDetected = "DDF_08";
    public const string UnsupportedFileType = "DDF_09";
    public const string FileTooLarge = "DDF_10";
    public const string NotFound = "DDF_11";

    private sealed record Definition(string Reason, bool Retriable, int StatusCode);

    private static readonly Dictionary<string, Definition> s_definitions = new()
    {
        [Other] = new("Any other reason", true, StatusCodes.Status500InternalServerError),
        [TracesUnavailable] = new("TRACES service unavailable", true, StatusCodes.Status502BadGateway),
        [TracesTimeout] = new("Document retrieval timeout", true, StatusCodes.Status504GatewayTimeout),
        [TracesRateLimited] = new("TRACES request rate limited", true, StatusCodes.Status502BadGateway),
        [UploaderFailure] = new("CDP uploader failure", true, StatusCodes.Status502BadGateway),
        [StorageFailure] = new("S3 storage failure", true, StatusCodes.Status502BadGateway),
        [InvalidCredentials] = new("Invalid credentials", false, StatusCodes.Status502BadGateway),
        [PermissionDenied] = new("Permission denied", false, StatusCodes.Status403Forbidden),
        [MalwareDetected] = new("Malware detected", false, StatusCodes.Status422UnprocessableEntity),
        [UnsupportedFileType] = new("Unsupported file type", false, StatusCodes.Status422UnprocessableEntity),
        [FileTooLarge] = new("File exceeds maximum size", false, StatusCodes.Status422UnprocessableEntity),
        [NotFound] = new("Document not found or no longer available", false, StatusCodes.Status404NotFound),
    };

    public static IReadOnlyCollection<string> All => s_definitions.Keys;

    /// <summary>
    /// Whether requesting the attachment again starts a new download. Codes not in the table, such as
    /// those recorded before it, count as <see cref="Other"/> and are retried.
    /// </summary>
    public static bool IsRetriable(string? code) => Find(code).Retriable;

    public static string ReasonFor(string? code) => Find(code).Reason;

    public static int ToStatusCode(string? code) => Find(code).StatusCode;

    private static Definition Find(string? code) =>
        code is not null && s_definitions.TryGetValue(code, out var definition) ? definition : s_definitions[Other];

    public static AttachmentError Create(
        string code,
        AttachmentErrorSource source,
        DateTime now,
        string? reason = null,
        string? detail = null
    ) =>
        new()
        {
            Source = source,
            Code = code,
            Message = reason ?? ReasonFor(code),
            Detail = detail,
            Retryable = IsRetriable(code),
            OccurredAt = now,
        };

    /// <summary>
    /// Classifies an exception thrown while downloading or handing off an attachment. Upstream
    /// messages go in <see cref="AttachmentError.Detail"/> only (ADR-0002 §4).
    /// </summary>
    public static AttachmentError FromException(Exception exception, DateTime now)
    {
        var detail = string.Join(" ---> ", Chain(exception).Select(e => $"{e.GetType().Name}: {e.Message}"));
        var (code, source) = Classify(exception);
        return Create(code, source, now, detail: detail);
    }

    private static (string Code, AttachmentErrorSource Source) Classify(Exception exception)
    {
        switch (exception)
        {
            case PermissionDeniedException:
                return (PermissionDenied, AttachmentErrorSource.Traces);
            case CdpUploaderException { StatusCode: HttpStatusCode.RequestEntityTooLarge }:
                return (FileTooLarge, AttachmentErrorSource.Uploader);
            case CdpUploaderException:
                return (UploaderFailure, AttachmentErrorSource.Uploader);
            case AmazonS3Exception:
                return (StorageFailure, AttachmentErrorSource.Storage);
        }

        var chain = Chain(exception).ToList();

        if (chain.Any(IsTimeout))
            return (TracesTimeout, AttachmentErrorSource.Traces);
        if (chain.Any(e => HasStatus(e, HttpStatusCode.TooManyRequests)))
            return (TracesRateLimited, AttachmentErrorSource.Traces);
        if (chain.Any(IsAuthenticationFailure))
            return (InvalidCredentials, AttachmentErrorSource.Traces);

        return exception switch
        {
            TracesCommunicationException => (TracesUnavailable, AttachmentErrorSource.Traces),
            InvalidSoapException => (Other, AttachmentErrorSource.Traces),
            _ => (Other, AttachmentErrorSource.Internal),
        };
    }

    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            yield return current;
    }

    private static bool IsTimeout(Exception exception) => exception is TimeoutException or TaskCanceledException;

    // WCF reports an unexpected HTTP status in the message, e.g. "(429) Too Many Requests".
    private static bool HasStatus(Exception exception, HttpStatusCode status) =>
        (exception is HttpRequestException { StatusCode: { } code } && code == status)
        || exception.Message.Contains($"({(int)status})", StringComparison.Ordinal);

    private static readonly string[] s_authenticationFaults =
    [
        "FailedAuthentication",
        "InvalidSecurity",
        "InvalidSecurityToken",
        "FailedCheck",
    ];

    // An HTTP 401 surfaces as MessageSecurityException; a rejected WS-Security header as a SOAP fault.
    private static bool IsAuthenticationFailure(Exception exception) =>
        exception is MessageSecurityException
        || HasStatus(exception, HttpStatusCode.Unauthorized)
        || (
            exception is FaultException fault
            && (
                s_authenticationFaults.Contains(fault.Code?.Name)
                || s_authenticationFaults.Contains(fault.Code?.SubCode?.Name)
            )
        );

    /// <summary>The attachment's type is not on <see cref="ChedAttachmentOptions.AllowedFileTypes"/>.</summary>
    public static AttachmentError Unsupported(
        string? contentType,
        string? fileName,
        IReadOnlyList<string> allowedMimeTypes,
        DateTime now
    ) =>
        Create(
            UnsupportedFileType,
            AttachmentErrorSource.Traces,
            now,
            $"Attachments of type '{contentType}' are not supported. Supported types: {string.Join(", ", allowedMimeTypes)}.",
            $"TRACES content type '{contentType}', file name '{fileName}'"
        );

    /// <summary>The scanner found the file is not the type it was declared as.</summary>
    public static AttachmentError ContentNotAllowed(
        string? declaredContentType,
        string? detectedContentType,
        DateTime now
    ) =>
        Create(
            UnsupportedFileType,
            AttachmentErrorSource.Scan,
            now,
            "The attachment's content does not match its file type.",
            $"Declared as '{declaredContentType}', detected content type '{detectedContentType}'"
        );

    /// <summary>Maps the CDP uploader's file error to a failure code.</summary>
    public static AttachmentError ScanRejected(string? uploaderCode, string? uploaderMessage, DateTime now)
    {
        var (code, source) = uploaderCode switch
        {
            CdpUploaderErrorCodes.FileVirus => (MalwareDetected, AttachmentErrorSource.Scan),
            CdpUploaderErrorCodes.FileTooLarge => (FileTooLarge, AttachmentErrorSource.Scan),
            CdpUploaderErrorCodes.FileInvalidType => (UnsupportedFileType, AttachmentErrorSource.Scan),
            // The uploader could not store the file in the bucket.
            CdpUploaderErrorCodes.FileUploadFailed => (StorageFailure, AttachmentErrorSource.Storage),
            CdpUploaderErrorCodes.FileEmpty => (Other, AttachmentErrorSource.Scan),
            _ => (UploaderFailure, AttachmentErrorSource.Uploader),
        };

        return Create(
            code,
            source,
            now,
            detail: $"CDP uploader error '{uploaderCode ?? "none"}': {uploaderMessage ?? "no message"}"
        );
    }
}
