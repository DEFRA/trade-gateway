using Microsoft.Extensions.DependencyInjection;
using TracesNT.Services;
using TracesNT.WebServices;

namespace TracesNT.Extensions
{
    public static class TracesClientRegistrationExtensions
    {
        public static IServiceCollection AddTracesNtClients(
            this IServiceCollection services
        )
        {
            services.AddTracesNtClient<ChedCertificatePortClient, ChedCertificatePort>(
                "ChedCertificateServiceV2",
                (binding, endpoint) => new ChedCertificatePortClient(binding, endpoint)
            );

            services.AddTracesNtClient<EuIntraCertificatePortClient, EuIntraCertificatePort>(
                "EuIntraCertificateServiceV1",
                (binding, endpoint) => new EuIntraCertificatePortClient(binding, endpoint)
            );

            services.AddTracesNtClient<ReferenceDataPortClient, ReferenceDataPort>(
                "ReferenceDataServiceV1",
                (binding, endpoint) => new ReferenceDataPortClient(binding, endpoint)
            );

            services.AddTransient<IChedCertificateService, ChedCertificateService>();

            services.AddTransient<IEuIntraCertificateService, EuIntraCertificateService>();

            services.AddTransient<IReferenceDataService, ReferenceDataService>();

            return services;
        }
    }
}
