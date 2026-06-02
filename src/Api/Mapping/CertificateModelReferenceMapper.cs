using ContractCertificateModelReference = Defra.TradeGateway.Api.Contract.ReferenceData.CertificateModelReference;
using SoapCertificateModelReference = TracesNT.WebServices.CertificateModelReference;

namespace Api.Mapping;

internal static class CertificateModelReferenceMapper
{
    internal static ContractCertificateModelReference? Map(SoapCertificateModelReference? source)
    {
        if (source is null)
            return null;

        return new()
        {
            ModelId = checked((int)source.modelId),
            ShortTitle = source.ShortTitle.Value,
            LongTitle = source.LongTitle.Value,
            CreatedOn = source.createdOn.ToUniversalTime(),
            UpdatedOn = source.updatedOnSpecified ? source.updatedOn.ToUniversalTime() : null,
        };
    }
}
