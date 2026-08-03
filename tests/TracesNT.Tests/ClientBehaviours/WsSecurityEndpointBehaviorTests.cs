using System.Net.Http;
using System.ServiceModel.Channels;
using TracesNT.ClientBehaviours;

namespace TracesNT.Tests.ClientBehaviours;

public class WsSecurityEndpointBehaviorTests
{
    [Fact]
    public void ApplyClientBehavior_AddsWsSecurityMessageInspector()
    {
        var sut = new WsSecurityEndpointBehavior(new TracesNtCredentials());
        var clientRuntime = WcfTestHelpers.CreateClientRuntime();

        sut.ApplyClientBehavior(WcfTestHelpers.CreateEndpoint(), clientRuntime);

        clientRuntime
            .ClientMessageInspectors.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<WsSecurityMessageInspector>();
    }
}
