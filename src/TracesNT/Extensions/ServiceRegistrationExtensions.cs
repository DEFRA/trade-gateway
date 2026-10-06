using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.ClientBehaviours;
using TracesNT.Services;

namespace TracesNT.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceRegistrationExtensions
{
    private static readonly ConcurrentDictionary<string, Binding> s_bindingCache = new();

    internal static IServiceCollection AddTracesNtClient<TClient, TChannel>(
        this IServiceCollection services,
        string servicePath,
        string credentialKey,
        Func<Binding, EndpointAddress, TClient> clientFactory,
        bool useSoap12Mtom = false
    )
        where TClient : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        services.AddScoped<TClient>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<TracesNtConfig>>().Value;
            var credentials = sp.GetRequiredService<IOptionsMonitor<TracesNtCredentials>>().Get(credentialKey);
            var logger = sp.GetRequiredService<ILogger<TClient>>();
            var endpoint = new EndpointAddress(config.GetServiceUrl(servicePath));
            var binding = useSoap12Mtom ? GetOrCreateSoap12MtomBinding(endpoint.Uri) : GetOrCreateBinding(endpoint.Uri);
            var metricsService = sp.GetRequiredService<ITracesNtClientMetricsService>();

            TClient client = clientFactory(binding, endpoint);

            // Logging runs before WS-Security so credentials are never captured in logs.
            // BeforeSendRequest fires in registration order; WS-Security adds its header last.
            client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehavior(logger));
            client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(credentials));
            client.Endpoint.EndpointBehaviors.Add(new MetricsEndpointBehaviour(metricsService, logger));

            return client;
        });

        return services;
    }

    private static Binding GetOrCreateBinding(Uri endpointUrl)
    {
        var proxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY") ?? string.Empty;
        var key = $"{endpointUrl.Scheme}|{proxyUrl}";

        return s_bindingCache.GetOrAdd(
            key,
            _ =>
            {
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

                    return (Binding)binding;
                }

                return new BasicHttpBinding(BasicHttpSecurityMode.None)
                {
                    MaxReceivedMessageSize = int.MaxValue,
                    MaxBufferPoolSize = int.MaxValue,
                };
            }
        );
    }

    /// <summary>
    /// Some TRACES NT services (e.g. CertificateAttachmentsServiceV1) publish a SOAP 1.2 binding with an
    /// MTOM policy and reject SOAP 1.1 text/xml requests with HTTP 415.
    /// </summary>
    private static Binding GetOrCreateSoap12MtomBinding(Uri endpointUrl)
    {
        var proxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY") ?? string.Empty;
        var key = $"soap12mtom|{endpointUrl.Scheme}|{proxyUrl}";

        return s_bindingCache.GetOrAdd(
            key,
            _ =>
            {
                var encoding = new MtomMessageEncodingBindingElement(MessageVersion.Soap12, Encoding.UTF8);
                XmlDictionaryReaderQuotas.Max.CopyTo(encoding.ReaderQuotas);

                var transport =
                    endpointUrl.Scheme == Uri.UriSchemeHttps
                        ? new HttpsTransportBindingElement()
                        : new HttpTransportBindingElement();
                transport.MaxReceivedMessageSize = int.MaxValue;
                transport.MaxBufferPoolSize = int.MaxValue;
                if (!string.IsNullOrEmpty(proxyUrl))
                {
                    transport.UseDefaultWebProxy = false;
                    transport.ProxyAddress = new Uri(proxyUrl);
                }

                return new CustomBinding(encoding, transport);
            }
        );
    }
}
