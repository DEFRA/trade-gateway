using Defra.TradeImports.CdpUploader;
using TracesNT.Exceptions;

namespace Api.Attachments;

/// <summary>
/// The failure codes stored on a <see cref="ChedAttachmentEntity"/> and returned to callers, with the
/// HTTP status each maps to. Scan codes are the CDP uploader's own (<c>FILE_VIRUS</c> and friends).
/// </summary>
public static class AttachmentErrorCodes
{
    public const string ChedNotFound = "CHED_NOT_FOUND";
    public const string AttachmentNotFound = "ATTACHMENT_NOT_FOUND";
    public const string TracesPermissionDenied = "TRACES_PERMISSION_DENIED";
    public const string TracesInvalidRequest = "TRACES_INVALID_REQUEST";
    public const string TracesUnavailable = "TRACES_UNAVAILABLE";
    public const string UploadFailed = "UPLOAD_FAILED";
    public const string StorageObjectMissing = "STORAGE_OBJECT_MISSING";
    public const string UnsupportedFileTypeCode = "UNSUPPORTED_FILE_TYPE";
    public const string UnexpectedError = "UNEXPECTED_ERROR";

    // CDP uploader scan outcomes, see https://github.com/DEFRA/cdp-uploader#error-handling
    public const string FileVirus = CdpUploaderErrorCodes.FileVirus;
    public const string FileEmpty = CdpUploaderErrorCodes.FileEmpty;
    public const string FileTooLarge = CdpUploaderErrorCodes.FileTooLarge;
    public const string FileInvalidType = CdpUploaderErrorCodes.FileInvalidType;
    public const string FileUploadFailed = CdpUploaderErrorCodes.FileUploadFailed;
    public const string FileDownloadFailed = CdpUploaderErrorCodes.FileDownloadFailed;

    public static int ToStatusCode(string code) =>
        code switch
        {
            ChedNotFound or AttachmentNotFound => StatusCodes.Status404NotFound,
            TracesPermissionDenied => StatusCodes.Status403Forbidden,
            FileVirus or FileEmpty or FileTooLarge or FileInvalidType or UnsupportedFileTypeCode =>
                StatusCodes.Status422UnprocessableEntity,
            TracesInvalidRequest or UnexpectedError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status502BadGateway,
        };

    public static AttachmentError NotFound(string code, string message, DateTime now) =>
        new()
        {
            Source = AttachmentErrorSource.Traces,
            Code = code,
            Message = message,
            // A draft CHED is reported as not found and may be submitted later, so ask again.
            Retryable = true,
            OccurredAt = now,
        };

    /// <summary>
    /// Classifies an exception thrown while downloading or handing off an attachment. Upstream
    /// messages go in <see cref="AttachmentError.Detail"/> only (ADR-0002 §4).
    /// </summary>
    public static AttachmentError FromException(Exception exception, DateTime now)
    {
        var detail = $"{exception.GetType().Name}: {exception.Message}";

        return exception switch
        {
            PermissionDeniedException => Build(
                AttachmentErrorSource.Traces,
                TracesPermissionDenied,
                "Access to this attachment is not permitted."
            ),
            InvalidSoapException => Build(
                AttachmentErrorSource.Traces,
                TracesInvalidRequest,
                "TRACES rejected the attachment request."
            ),
            TracesCommunicationException => Build(
                AttachmentErrorSource.Traces,
                TracesUnavailable,
                "An error occurred communicating with TRACES."
            ),
            CdpUploaderException => Build(
                AttachmentErrorSource.Uploader,
                UploadFailed,
                "An error occurred handing the attachment to the file uploader."
            ),
            _ => Build(AttachmentErrorSource.Internal, UnexpectedError, "An unexpected error occurred."),
        };

        AttachmentError Build(AttachmentErrorSource source, string code, string message) =>
            new()
            {
                Source = source,
                Code = code,
                Message = message,
                Detail = detail,
                Retryable = true,
                OccurredAt = now,
            };
    }

    /// <summary>
    /// Whether the consumer should rethrow, so SQS redelivers the message, rather than settle on the
    /// failure. Anything not explained by TRACES' answer is treated as transient.
    /// </summary>
    public static bool IsTransient(AttachmentError error) =>
        error.Code is TracesUnavailable or UploadFailed or UnexpectedError;

    /// <summary>
    /// The attachment's type is not on <see cref="ChedAttachmentOptions.AllowedFileTypes"/>. Not retried
    /// on its own; <see cref="ChedAttachmentService"/> asks again once the list allows it.
    /// </summary>
    public static AttachmentError UnsupportedFileType(
        string? contentType,
        string? fileName,
        IReadOnlyList<string> allowedMimeTypes,
        DateTime now
    ) =>
        new()
        {
            Source = AttachmentErrorSource.Traces,
            Code = UnsupportedFileTypeCode,
            Message =
                $"Attachments of type '{contentType}' are not supported. Supported types: {string.Join(", ", allowedMimeTypes)}.",
            Detail = $"TRACES content type '{contentType}', file name '{fileName}'",
            Retryable = false,
            OccurredAt = now,
        };

    /// <summary>The scanner found the file is not the type it was declared as.</summary>
    public static AttachmentError ContentNotAllowed(string? detectedContentType, DateTime now) =>
        new()
        {
            Source = AttachmentErrorSource.Scan,
            Code = FileInvalidType,
            Message = "The attachment's content is not a supported file type.",
            Detail = $"Detected content type '{detectedContentType}'",
            Retryable = false,
            OccurredAt = now,
        };

    public static AttachmentError ScanRejected(string? code, string? message, DateTime now)
    {
        var errorCode = string.IsNullOrWhiteSpace(code) ? FileUploadFailed : code;
        return new AttachmentError
        {
            Source = AttachmentErrorSource.Scan,
            Code = errorCode,
            Message = string.IsNullOrWhiteSpace(message) ? "The attachment was rejected by the file scan." : message,
            // A rejected file will be rejected again; only the uploader's own failures are worth retrying.
            Retryable = !CdpUploaderErrorCodes.IsFileRejection(errorCode),
            OccurredAt = now,
        };
    }
}
