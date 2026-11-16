using System.Diagnostics.Metrics;
using Api.Attachments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TracesNT.Services;

namespace Api.Tests.Attachments;

public sealed class AttachmentFailureRecorderTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc);

    private readonly ServiceProvider _services;
    private readonly FakeLogger<AttachmentFailureRecorder> _logger = new();
    private readonly MetricCollector<long> _failures;
    private readonly AttachmentFailureRecorder _recorder;

    public AttachmentFailureRecorderTests()
    {
        _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var meterFactory = _services.GetRequiredService<IMeterFactory>();
        _failures = new MetricCollector<long>(
            meterFactory,
            TracesNtClientMetricsService.MeterName,
            AttachmentFailureRecorder.InstrumentName
        );
        _recorder = new AttachmentFailureRecorder(meterFactory, _logger);
    }

    public void Dispose()
    {
        _failures.Dispose();
        _services.Dispose();
    }

    private static ChedAttachmentEntity Record()
    {
        var record = ChedAttachmentEntity.NewRequest("CHEDA.GB.2026.0000001", 7, Now);
        record.Attempts = 2;
        return record;
    }

    [Fact]
    public void Record_CountsTheFailureTaggedWithItsCodeAndReason()
    {
        var error = AttachmentFailureCodes.Create(
            AttachmentFailureCodes.TracesTimeout,
            AttachmentErrorSource.Traces,
            Now,
            detail: "TimeoutException: The request channel timed out"
        );

        _recorder.Record(Record(), error, AttachmentFailureStage.Download);

        var measurement = Assert.Single(_failures.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("DDF_02", measurement.Tags[AttachmentFailureRecorder.Tags.FailureCode]);
        Assert.Equal("Document retrieval timeout", measurement.Tags[AttachmentFailureRecorder.Tags.FailureReason]);
        Assert.Equal("Traces", measurement.Tags[AttachmentFailureRecorder.Tags.FailureSource]);
        Assert.Equal("Download", measurement.Tags[AttachmentFailureRecorder.Tags.Stage]);
        Assert.True((bool)measurement.Tags[AttachmentFailureRecorder.Tags.Retriable]!);
    }

    [Fact]
    public void Record_TagsTheCodesFixedReason_NotACustomMessage()
    {
        // A custom message can carry ids or file names, which would make the metric unbounded.
        var error = AttachmentFailureCodes.Unsupported("text/plain", "notes.txt", ["application/pdf"], Now);

        _recorder.Record(Record(), error, AttachmentFailureStage.Download);

        var measurement = Assert.Single(_failures.GetMeasurementSnapshot());
        Assert.Equal("Unsupported file type", measurement.Tags[AttachmentFailureRecorder.Tags.FailureReason]);
        Assert.False((bool)measurement.Tags[AttachmentFailureRecorder.Tags.Retriable]!);
    }

    [Fact]
    public void Record_LogsAWarningWithTheFailureDetails()
    {
        var error = AttachmentFailureCodes.ScanRejected("FILE_VIRUS", "The selected file contains a virus", Now);

        _recorder.Record(Record(), error, AttachmentFailureStage.Scan);

        var log = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        var state = log.StructuredState!.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal("CHEDA.GB.2026.0000001/7", state["Id"]);
        Assert.Equal("Scan", state["Stage"]);
        Assert.Equal("DDF_08", state["FailureCode"]);
        Assert.Equal("Malware detected", state["FailureReason"]);
        Assert.Equal("Scan", state["FailureSource"]);
        Assert.Equal("False", state["Retriable"]);
        Assert.Equal("2", state["Attempts"]);
        Assert.Contains("FILE_VIRUS", state["Detail"]);
    }
}
