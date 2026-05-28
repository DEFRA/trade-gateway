using Microsoft.Extensions.DependencyInjection;
using TracesNT.Services;
using TracesNT.WebServices;

namespace TracesNT.Extensions
{
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

            services.AddTracesNtClient<ReferenceDataPortClient, ReferenceDataPort>(
                "ReferenceDataServiceV1",
                xApiKey
            );

            services.AddTransient<IEuIntraCertificateService, EuIntraCertificateService>();

            services.AddTransient<IReferenceDataService, ReferenceDataService>();

            return services;
        }
    }
}
