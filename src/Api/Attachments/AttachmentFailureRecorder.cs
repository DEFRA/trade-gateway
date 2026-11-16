using System.Diagnostics;
using System.Diagnostics.Metrics;
using TracesNT.Services;

namespace Api.Attachments;

/// <summary>
/// Where in the request a CHED attachment failure was recorded. A metric tag, so failures can be told
/// apart from the stage that saw them.
/// </summary>
public enum AttachmentFailureStage
{
    /// <summary>Downloading from TRACES or handing the file to the CDP uploader.</summary>
    Download,

    /// <summary>The CDP uploader's scan-results callback.</summary>
    Scan,

    /// <summary>Reading the stored file from S3 to serve it.</summary>
    Retrieval,
}

public interface IAttachmentFailureRecorder
{
    /// <summary>
    /// Logs a failed TRACES digital document (CHED attachment) request and counts it on the
    /// <c>AttachmentRequestFailed</c> metric, tagged with the failure code and reason.
    /// </summary>
    void Record(ChedAttachmentEntity record, AttachmentError error, AttachmentFailureStage stage);
}

public sealed class AttachmentFailureRecorder : IAttachmentFailureRecorder
{
    public const string InstrumentName = "AttachmentRequestFailed";

    private readonly Counter<long> _failures;
    private readonly ILogger<AttachmentFailureRecorder> _logger;

    public AttachmentFailureRecorder(IMeterFactory meterFactory, ILogger<AttachmentFailureRecorder> logger)
    {
        _logger = logger;
        // The meter the EMF exporter publishes (see Program.UseEmfExporter).
        var meter = meterFactory.Create(TracesNtClientMetricsService.MeterName);
        _failures = meter.CreateCounter<long>(
            InstrumentName,
            "{failure}",
            "Failed TRACES digital document (CHED attachment) requests"
        );
    }

    public void Record(ChedAttachmentEntity record, AttachmentError error, AttachmentFailureStage stage)
    {
        _logger.LogWarning(
            "CHED attachment {Id} request failed at {Stage} with {FailureCode} ({FailureReason}) from {FailureSource}, "
                + "retriable {Retriable}, attempt {Attempts}: {Detail}",
            record.Id,
            stage,
            error.Code,
            error.Message,
            error.Source,
            error.Retryable,
            record.Attempts,
            error.Detail
        );

        // Bounded tags only: the reason is the code's fixed reason, not the error's (possibly detailed) message.
        _failures.Add(
            1,
            new TagList
            {
                { Tags.FailureCode, error.Code },
                { Tags.FailureReason, AttachmentFailureCodes.ReasonFor(error.Code) },
                { Tags.FailureSource, error.Source.ToString() },
                { Tags.Stage, stage.ToString() },
                { Tags.Retriable, error.Retryable },
            }
        );
    }

    public static class Tags
    {
        public const string FailureCode = "FailureCode";
        public const string FailureReason = "FailureReason";
        public const string FailureSource = "FailureSource";
        public const string Stage = "Stage";
        public const string Retriable = "Retriable";
    }
}
