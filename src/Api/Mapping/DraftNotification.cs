using Microsoft.Extensions.Logging;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class DraftNotification
{
    internal const string DraftStatusCode = "47";

    internal static bool IsDraft(SPSCertificateType? certificate) =>
        IsDraft(certificate?.SPSExchangedDocument?.StatusCode);

    internal static bool IsDraft(ChedCertificateQueryResultType? result) => IsDraft(result?.Status);

    internal static bool IsDraft(EuIntraCertificateQueryResultType? result) => IsDraft(result?.Status);

    internal static void LogIgnored(ILoggerFactory loggerFactory, string reference) =>
        loggerFactory
            .CreateLogger(typeof(DraftNotification).FullName!)
            .LogInformation(
                "{Reference} has not been processed due to current status of {StatusCode}",
                reference,
                DraftStatusCode
            );

    private static bool IsDraft(StatusCodeType? status) =>
        status is not null && status.Value.XmlEnumCode() == DraftStatusCode;
}
