using TracesNT.ClientBehaviours;

namespace TracesNT.Tests.ClientBehaviours;

public class LoggingEndpointBehaviorTests
{
    [Fact]
    public void ApplyClientBehavior_AddsLoggingMessageInspector()
    {
        var logger = new TestLogger();
        var sut = new LoggingEndpointBehavior(logger);
        var clientRuntime = WcfTestHelpers.CreateClientRuntime();

        sut.ApplyClientBehavior(WcfTestHelpers.CreateEndpoint(), clientRuntime);

        clientRuntime.ClientMessageInspectors.Should().ContainSingle()
            .Which.Should().BeOfType<LoggingMessageInspector>();
    }
}
