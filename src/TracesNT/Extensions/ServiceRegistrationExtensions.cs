using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.ClientBehaviours;

namespace TracesNT.Extensions;

public static class ServiceRegistrationExtensions
{
    internal static IServiceCollection AddTracesNtClient<TClient, TChannel>(
        this IServiceCollection services,
        Uri endpointUrl,
        string? xApiKey
    )
        where TClient : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        var endpoint = new EndpointAddress(endpointUrl);
        var proxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY");

        Binding binding;
        if (endpoint.Uri.Scheme == Uri.UriSchemeHttps)
        {
            var b = new BasicHttpsBinding(BasicHttpsSecurityMode.Transport)
            {
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
            };
            b.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                b.UseDefaultWebProxy = false;
                b.ProxyAddress = new Uri(proxyUrl);
            }
            binding = b;
        }
        else
        {
            binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
            };
        }

        return services.RegisterClient<TClient, TChannel>(binding, endpoint, xApiKey);
    }

    // SOAP 1.2 without MTOM — binary content sent as inline base64 rather than XOP MIME parts.
    internal static IServiceCollection AddTracesNtClientSoap12<TClient, TChannel>(
        this IServiceCollection services,
        Uri endpointUrl,
        string? xApiKey
    )
        where TClient : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        var endpoint = new EndpointAddress(endpointUrl);
        var proxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY");
        var version = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None);
        var encoding = new TextMessageEncodingBindingElement(version, Encoding.UTF8);
        var transport = new HttpsTransportBindingElement
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferPoolSize = int.MaxValue,
            AuthenticationScheme = AuthenticationSchemes.Anonymous,
        };
        if (!string.IsNullOrEmpty(proxyUrl))
        {
            transport.UseDefaultWebProxy = false;
            transport.ProxyAddress = new Uri(proxyUrl);
        }

        return services.RegisterClient<TClient, TChannel>(
            new CustomBinding(encoding, transport),
            endpoint,
            xApiKey
        );
    }

    private static IServiceCollection RegisterClient<TClient, TChannel>(
        this IServiceCollection services,
        Binding binding,
        EndpointAddress endpoint,
        string? xApiKey
    )
        where TClient : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        services.AddTransient<TClient>((sp) =>
        {
            var config = sp.GetRequiredService<IOptions<TracesNtConfig>>();
            var logger = sp.GetRequiredService<ILogger<TClient>>();

            var client = (TClient)Activator.CreateInstance(typeof(TClient), binding, endpoint)!;

            // Logging runs before WS-Security so credentials are never captured in logs.
            // BeforeSendRequest fires in registration order; WS-Security adds its header last.
            client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehavior(logger));
            client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(config.Value, xApiKey));

            return client;
        });
 
        return services;
    }
}
