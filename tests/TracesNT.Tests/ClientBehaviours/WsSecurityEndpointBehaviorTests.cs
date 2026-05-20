using System.Net.Http;
using System.ServiceModel.Channels;
using TracesNT.ClientBehaviours;

namespace TracesNT.Tests.ClientBehaviours;

public class WsSecurityEndpointBehaviorTests
{
    [Fact]
    public void ApplyClientBehavior_AddsWsSecurityMessageInspector()
    {
        var sut = new WsSecurityEndpointBehavior(new TracesNtConfig(), "api-key");
        var clientRuntime = WcfTestHelpers.CreateClientRuntime();

        sut.ApplyClientBehavior(WcfTestHelpers.CreateEndpoint(), clientRuntime);

        clientRuntime.ClientMessageInspectors.Should().ContainSingle()
            .Which.Should().BeOfType<WsSecurityMessageInspector>();
    }

    [Fact]
    public void AddBindingParameters_AddsCustomHeaderDelegatingHandlerFactory()
    {
        var sut = new WsSecurityEndpointBehavior(new TracesNtConfig(), "api-key");
        var parameters = new BindingParameterCollection();

        sut.AddBindingParameters(WcfTestHelpers.CreateEndpoint(), parameters);

        parameters.Should().ContainSingle();
        var factory = parameters[0].Should().BeOfType<Func<HttpClientHandler, HttpMessageHandler>>().Subject;
        var innerHandler = new HttpClientHandler();
        var handler = factory(innerHandler);

        handler.Should().BeOfType<CustomHeaderDelegatingHandler>();
        ((DelegatingHandler)handler).InnerHandler.Should().BeSameAs(innerHandler);
    }
}
