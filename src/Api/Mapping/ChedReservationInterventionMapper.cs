using Trade.Gateway.Api.Contract.Customs;

namespace Api.Mapping;

public static class ChedReservationInterventionMapper
{
    public static TracesNT.WebServices.ChedInterventionRequestType ToChedInterventionRequestType(
        this ChedReservationInterventionRequest source
    )
    {
        return new TracesNT.WebServices.ChedInterventionRequestType
        {
            CompetentCustomsOffice = new TracesNT.WebServices.CompetentCustomsOfficeType
            {
                ReferenceNumber = source.CompetentCustomsOffice.ReferenceNumber,
            },

            SendingDate = source.SendingDate,
            CustomsDocumentReference = source.CustomsDocumentReference,
            TARICDocument = source.TaricDocument,
            ChedCertificateId = source.ChedCertificateId,

            ConsignmentItem = source.ConsignmentItems.Select(x => x.ToCertex()).ToArray(),

            InterventionType = source.InterventionType.ToCertex(),
        };
    }

    private static TracesNT.WebServices.ConsignmentItemR6ForInterventionType ToCertex(
        this CustomsConsignmentItem source
    )
    {
        var result = new TracesNT.WebServices.ConsignmentItemR6ForInterventionType
        {
            GoodsItemNumber = source.GoodsItemNumber.ToString(),
            CertificateLineNumber = source.CertificateLineNumber.ToString(),
            ClassCode = source.ClassCode,
        };

        if (source.NetWeightQuantity.HasValue)
        {
            result.NetWeightQuantity = source.NetWeightQuantity.Value;
            result.NetWeightQuantitySpecified = true;
        }

        if (source.NetWeightUnitOfMeasure.HasValue)
        {
            result.NetWeightUnitOfMeasure = source.NetWeightUnitOfMeasure.Value.ToCertex();
            result.NetWeightUnitOfMeasureSpecified = true;
        }

        if (source.NetVolumeQuantity.HasValue)
        {
            result.NetVolumeQuantity = source.NetVolumeQuantity.Value;
            result.NetVolumeQuantitySpecified = true;
        }

        if (source.NetVolumeUnitOfMeasure.HasValue)
        {
            result.NetVolumeUnitOfMeasure = source.NetVolumeUnitOfMeasure.Value.ToCertex();
            result.NetVolumeUnitOfMeasureSpecified = true;
        }

        return result;
    }

    private static TracesNT.WebServices.UniversalUnitOfMeasureType ToCertex(this UnitOfMeasureType source)
    {
        return Enum.Parse<TracesNT.WebServices.UniversalUnitOfMeasureType>(source.ToString());
    }

    private static TracesNT.WebServices.InterventionMessageInformationType ToCertex(this InterventionType source)
    {
        return source switch
        {
            InterventionType.DocumentCheck => TracesNT.WebServices.InterventionMessageInformationType.Item01,

            InterventionType.IdentityCheck => TracesNT.WebServices.InterventionMessageInformationType.Item02,

            InterventionType.PhysicalCheck => TracesNT.WebServices.InterventionMessageInformationType.Item03,

            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }
}
