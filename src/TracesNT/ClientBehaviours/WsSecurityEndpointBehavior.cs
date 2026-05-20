using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace TracesNT.ClientBehaviours;

public class WsSecurityEndpointBehavior(TracesNtConfig config, string? xApiKey) : IEndpointBehavior
{
    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) =>
        clientRuntime.ClientMessageInspectors.Add(new WsSecurityMessageInspector(config));
    
    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
        bindingParameters.Add(new Func<HttpClientHandler, HttpMessageHandler>(x => new CustomHeaderDelegatingHandler(x, xApiKey)));
    }

    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

    public void Validate(ServiceEndpoint endpoint) { }
}