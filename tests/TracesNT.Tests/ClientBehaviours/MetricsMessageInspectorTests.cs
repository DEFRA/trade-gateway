using System.Diagnostics;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using NSubstitute;
using TracesNT.ClientBehaviours;
using TracesNT.Services;

namespace TracesNT.Tests.ClientBehaviours;

public class MetricsMessageInspectorTests
{
    private readonly IClientChannel _channel = Substitute.For<IClientChannel>();
    private readonly ITracesNtClientMetricsService _metricsService = Substitute.For<ITracesNtClientMetricsService>();
    private readonly ILogger _logger = new TestLogger(LogLevel.Debug);
    private readonly MetricsMessageInspector _sut;

    public MetricsMessageInspectorTests()
    {
        _sut = new MetricsMessageInspector(_metricsService, _logger);
    }

    [Fact]
    public void BeforeSendRequest_ShouldReturnRequestedActionAndStartActionTimer()
    {
        var request = Message.CreateMessage(MessageVersion.Soap11, "some-tracesnt-action");

        var result = _sut.BeforeSendRequest(ref request, _channel);

        result.Should().NotBeNull();

        var stopWatch = result.GetType().GetProperty("Stopwatch")?.GetValue(result, null);
        stopWatch.Should().NotBeNull();
        stopWatch.Should().BeOfType<Stopwatch>();
        ((Stopwatch)stopWatch).IsRunning.Should().BeTrue();

        var action = result.GetType().GetProperty("Action")?.GetValue(result, null);
        action.Should().Be("some-tracesnt-action");
    }

    [Fact]
    public void AfterReceiveReply_ShouldRecordRequest()
    {
        var request = Message.CreateMessage(MessageVersion.Soap11, "some-tracesnt-action");
        var correlationState = _sut.BeforeSendRequest(ref request, _channel);
        var reply = Message.CreateMessage(MessageVersion.Soap11, "some-tracesnt-reply");
        var httpResponse = new HttpResponseMessageProperty();
        httpResponse.StatusCode = HttpStatusCode.OK;
        reply.Properties.Add("httpResponse", httpResponse);

        _sut.AfterReceiveReply(ref reply, correlationState);

        var stopWatch = correlationState.GetType().GetProperty("Stopwatch")?.GetValue(correlationState, null);
        ((Stopwatch)stopWatch!).IsRunning.Should().BeFalse();

        _metricsService
            .Received(1)
            .RecordRequest(Arg.Is("some-tracesnt-action"), Arg.Any<long>(), Arg.Is(200), Arg.Any<string>());
    }

    [Fact]
    public void AfterReceiveReply_WhenReplyIsFaulted_ShouldRecordRequest()
    {
        var request = Message.CreateMessage(MessageVersion.Soap11, "some-tracesnt-action");
        var correlationState = _sut.BeforeSendRequest(ref request, _channel);
        var reply = Message.CreateMessage(
            MessageVersion.Soap11,
            new FaultCode("Sender"),
            "Some TracesNT error",
            "some-tracesnt-reply"
        );
        var httpResponse = new HttpResponseMessageProperty();
        httpResponse.StatusCode = HttpStatusCode.OK;
        reply.Properties.Add("httpResponse", httpResponse);

        _sut.AfterReceiveReply(ref reply, correlationState);

        var stopWatch = correlationState.GetType().GetProperty("Stopwatch")?.GetValue(correlationState, null);
        ((Stopwatch)stopWatch!).IsRunning.Should().BeFalse();

        _metricsService
            .Received(1)
            .RecordRequest(Arg.Is("some-tracesnt-action"), Arg.Any<long>(), Arg.Is(200), Arg.Is("Client"));
    }

    [Fact]
    public void AfterReceiveReply_WhenExceptionOccurs_ShouldLogAnError()
    {
        var request = Message.CreateMessage(MessageVersion.Soap11, "some-tracesnt-action");
        var correlationState = _sut.BeforeSendRequest(ref request, _channel);
        var reply = Message.CreateMessage(MessageVersion.Soap11, "some-tracesnt-reply");
        var httpResponse = new HttpResponseMessageProperty();
        httpResponse.StatusCode = HttpStatusCode.OK;
        reply.Properties.Add("httpResponse", httpResponse);
        _metricsService
            .When(x => x.RecordRequest(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<int>(), Arg.Any<string>()))
            .Throw(new Exception("A test exception"));

        _sut.AfterReceiveReply(ref reply, correlationState);

        ((TestLogger)_logger)
            .Entries.Should()
            .Contain(entry =>
                entry.Level == LogLevel.Error && entry.Message.Contains("Failed to publish TracesNT metrics")
            );
    }
}
