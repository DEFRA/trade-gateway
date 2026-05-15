using System.ServiceModel;
using System.ServiceModel.Channels;
using NSubstitute;
using TracesNT.ClientBehaviours;

namespace TracesNT.Tests.ClientBehaviours;

public class LoggingMessageInspectorTests
{
    private readonly IClientChannel _channel = Substitute.For<IClientChannel>();

    [Fact]
    public void BeforeSendRequest_WhenDebugEnabled_LogsRequestAction()
    {
        var logger = new TestLogger(LogLevel.Debug);
        var sut = new LoggingMessageInspector(logger);
        var request = Message.CreateMessage(MessageVersion.Soap11, "urn:test-action");

        var correlationState = sut.BeforeSendRequest(ref request, _channel);

        correlationState.Should().NotBeNull();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("SOAP Request: Action=urn:test-action")
        );
    }

    [Fact]
    public void AfterReceiveReply_WhenReplyIsNotFault_LogsResponse()
    {
        var logger = new TestLogger(LogLevel.Information);
        var sut = new LoggingMessageInspector(logger);
        var request = Message.CreateMessage(MessageVersion.Soap11, "urn:test-action");
        var correlationState = sut.BeforeSendRequest(ref request, _channel);
        var reply = Message.CreateMessage(MessageVersion.Soap11, "urn:test-reply");

        sut.AfterReceiveReply(ref reply, correlationState!);

        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("Service Response: Action=urn:test-action")
        );
    }

    [Fact]
    public void AfterReceiveReply_WhenReplyIsFault_LogsFaultAndPreservesReply()
    {
        var logger = new TestLogger(LogLevel.Error);
        var sut = new LoggingMessageInspector(logger);
        var request = Message.CreateMessage(MessageVersion.Soap11, "urn:test-action");
        var correlationState = sut.BeforeSendRequest(ref request, _channel);
        var reply = Message.CreateMessage(
            MessageVersion.Soap11,
            new FaultCode("Sender"),
            "Boom",
            "urn:test-reply"
        );

        sut.AfterReceiveReply(ref reply, correlationState!);

        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error &&
            entry.Message.Contains("Service Fault: Action=urn:test-action") &&
            entry.Message.Contains("Code=Client") &&
            entry.Message.Contains("Reason=Boom")
        );

        var fault = MessageFault.CreateFault(reply, int.MaxValue);
        fault.Code.Name.Should().Be("Client");
        fault.Reason.ToString().Should().Be("Boom");
    }
}
