using NSubstitute;
using TracesNT.ClientBehaviours;
using TracesNT.Services;

namespace TracesNT.Tests.ClientBehaviours;

public class MetricsEndpointBehaviourTests
{
    [Fact]
    public void ApplyClientBehavior_AddsMetricsMessageInspector()
    {
        var metricsService = Substitute.For<ITracesNtClientMetricsService>();
        var logger = new TestLogger();
        var sut = new MetricsEndpointBehaviour(metricsService, logger);
        var clientRuntime = WcfTestHelpers.CreateClientRuntime();
    
        sut.ApplyClientBehavior(WcfTestHelpers.CreateEndpoint(), clientRuntime);
    
        clientRuntime
            .ClientMessageInspectors.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<MetricsMessageInspector>();
    }
}