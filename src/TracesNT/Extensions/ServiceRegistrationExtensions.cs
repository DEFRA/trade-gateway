using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.ClientBehaviours;

namespace TracesNT.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceRegistrationExtensions
{
    internal static IServiceCollection AddTracesNtClient<TClient, TChannel>(
        this IServiceCollection services,
        string servicePath,
        string? xApiKey
    )
        where TClient : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        return services.RegisterClient<TClient, TChannel>(
            config => config.GetServiceUrl(servicePath),
            CreateBasicBinding,
            xApiKey
        );
    }

    // SOAP 1.2 without MTOM — binary content sent as inline base64 rather than XOP MIME parts.
    internal static IServiceCollection AddTracesNtClientSoap12<TClient, TChannel>(
        this IServiceCollection services,
        string servicePath,
        string? xApiKey
    )
        where TClient : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        return services.RegisterClient<TClient, TChannel>(
            config => config.GetServiceUrl(servicePath),
            CreateSoap12Binding,
            xApiKey
        );
    }

    private static IServiceCollection RegisterClient<TClient, TChannel>(
        this IServiceCollection services,
        Func<TracesNtConfig, Uri> endpointFactory,
        Func<Uri, Binding> bindingFactory,
        string? xApiKey
    )
        where TClient : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        services.AddTransient<TClient>((sp) =>
        {
            var config = sp.GetRequiredService<IOptions<TracesNtConfig>>().Value;
            var logger = sp.GetRequiredService<ILogger<TClient>>();
            var endpoint = new EndpointAddress(endpointFactory(config));
            var binding = bindingFactory(endpoint.Uri);

            var client = (TClient)Activator.CreateInstance(typeof(TClient), binding, endpoint)!;

            // Logging runs before WS-Security so credentials are never captured in logs.
            // BeforeSendRequest fires in registration order; WS-Security adds its header last.
            client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehavior(logger));
            client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(config, xApiKey));

            return client;
        });
  
        return services;
    }

    private static Binding CreateBasicBinding(Uri endpointUrl)
    {
        var proxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (endpointUrl.Scheme == Uri.UriSchemeHttps)
        {
            var binding = new BasicHttpsBinding(BasicHttpsSecurityMode.Transport)
            {
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
            };
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                binding.UseDefaultWebProxy = false;
                binding.ProxyAddress = new Uri(proxyUrl);
            }

            return binding;
        }

        return new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferPoolSize = int.MaxValue,
        };
    }

    private static Binding CreateSoap12Binding(Uri endpointUrl)
    {
        var proxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY");
        var version = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None);
        var encoding = new TextMessageEncodingBindingElement(version, Encoding.UTF8);
        var transport = endpointUrl.Scheme == Uri.UriSchemeHttps
            ? new HttpsTransportBindingElement
            {
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                AuthenticationScheme = AuthenticationSchemes.Anonymous,
            }
            : new HttpTransportBindingElement
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

        return new CustomBinding(encoding, transport);
    }
}
