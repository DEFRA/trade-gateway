using Microsoft.Extensions.DependencyInjection;
using TracesNT.WebServices;

namespace TracesNT.Extensions
{
    public static class TracesClientRegistrationExtensions
    {
        public static IServiceCollection AddTracesNtClients(
            this IServiceCollection services,
            TracesNtConfig tracesNtConfig,
            string xApiKey
        )
        {
            services.AddTracesNtClient<EuIntraCertificatePortClient, EuIntraCertificatePort>(
                tracesNtConfig.GetServiceUrl("EuIntraCertificateServiceV1"),
                xApiKey
            );

            return services;
        }
    }
}
