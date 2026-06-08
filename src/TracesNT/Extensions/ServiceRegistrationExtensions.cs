using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using System.ServiceModel.Channels;
using TracesNT.ClientBehaviours;
using System.Collections.Concurrent;

namespace TracesNT.Extensions;

public static class ServiceRegistrationExtensions
{
    private static readonly ConcurrentDictionary<string, Binding> s_bindingCache = new();

    internal static IServiceCollection AddTracesNtClient<TClient, TChannel>(
        this IServiceCollection services,
        string servicePath,
            string? xApiKey,
            Func<Binding, EndpointAddress, TClient> clientFactory
        )
            where TClient : ClientBase<TChannel>, TChannel
            where TChannel : class
        {
            services.AddScoped<TClient>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<TracesNtConfig>>().Value;
                var logger = sp.GetRequiredService<ILogger<TClient>>();
                var endpoint = new EndpointAddress(config.GetServiceUrl(servicePath));
                var binding = GetOrCreateBinding(endpoint.Uri);

                TClient client = clientFactory(binding, endpoint);

                // Logging runs before WS-Security so credentials are never captured in logs.
                // BeforeSendRequest fires in registration order; WS-Security adds its header last.
                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehavior(logger));
                client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(config, xApiKey));

                return client;
            });

            return services;

    }

    private static Binding GetOrCreateBinding(Uri endpointUrl)
    {
        var proxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY") ?? string.Empty;
        var key = $"{endpointUrl.Scheme}|{proxyUrl}";

        return s_bindingCache.GetOrAdd(key, _ =>
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
        });
    }
}
