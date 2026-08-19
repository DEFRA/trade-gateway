using Microsoft.Extensions.DependencyInjection;
using TracesNT.Services;
using TracesNT.WebServices;

namespace TracesNT.Extensions
{
    public static class TracesClientRegistrationExtensions
    {
        public static IServiceCollection AddTracesNtClients(this IServiceCollection services)
        {
            services.AddTracesNtClient<ChedCertificatePortClient, ChedCertificatePort>(
                "ChedCertificateServiceV2",
                TracesNtCredentialKeys.Default,
                (binding, endpoint) => new ChedCertificatePortClient(binding, endpoint)
            );

            services.AddTracesNtClient<EuIntraCertificatePortClient, EuIntraCertificatePort>(
                "EuIntraCertificateServiceV1",
                TracesNtCredentialKeys.Default,
                (binding, endpoint) => new EuIntraCertificatePortClient(binding, endpoint)
            );

            services.AddTracesNtClient<DocomCertificateRetrievalPortClient, DocomCertificateRetrievalPort>(
                "DocomCertificateRetrievalServiceV1",
                TracesNtCredentialKeys.Default,
                (binding, endpoint) => new DocomCertificateRetrievalPortClient(binding, endpoint)
            );

            services.AddTracesNtClient<ReferenceDataPortClient, ReferenceDataPort>(
                "ReferenceDataServiceV1",
                TracesNtCredentialKeys.Default,
                (binding, endpoint) => new ReferenceDataPortClient(binding, endpoint)
            );

            services.AddTracesNtClient<CustomsCertexChedPortClient, CustomsCertexChedPort>(
                "CustomsCertexChedServiceV06",
                TracesNtCredentialKeys.Customs,
                (binding, endpoint) => new CustomsCertexChedPortClient(binding, endpoint)
            );

            services.AddTransient<IChedCertificateService, ChedCertificateService>();

            services.AddTransient<IEuIntraCertificateService, EuIntraCertificateService>();

            services.AddTransient<IDocomCertificateService, DocomCertificateService>();

            services.AddTransient<IReferenceDataService, ReferenceDataService>();

            services.AddTransient<ICustomsChedService, CustomsChedService>();

            return services;
        }
    }
}
