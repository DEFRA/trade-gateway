using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using TracesNT.Services;
using TracesNT.WebServices;

namespace TracesNT.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class TracesClientRegistrationExtensions
    {
        public static IServiceCollection AddTracesNtClients(
            this IServiceCollection services,
            string xApiKey
        )
        {
            services.AddTracesNtClient<EuIntraCertificatePortClient, EuIntraCertificatePort>(
                "EuIntraCertificateServiceV1",
                xApiKey
            );

            services.AddTransient<IEuIntraCertificateService, EuIntraCertificateService>();

            return services;
        }
    }
}
