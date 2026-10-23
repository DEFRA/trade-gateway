using TracesNT.WebServices;

namespace Api.Mapping;

internal static class DraftNotification
{
    internal const string DraftStatusCode = "47";

    internal static bool IsDraft(SPSCertificateType? certificate) =>
        certificate?.SPSExchangedDocument?.StatusCode?.Value.XmlEnumCode() == DraftStatusCode;

    internal static void LogIgnored(ILoggerFactory loggerFactory, string type, string reference) =>
        loggerFactory
            .CreateLogger(typeof(DraftNotification).FullName!)
            .LogInformation(
                "{Type} {Reference} has not been processed due to current status of {StatusCode}",
                type,
                reference,
                DraftStatusCode
            );
}
