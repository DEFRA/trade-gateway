using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Mapping;

public static class ChedReservationInterventionMapper
{
    internal static IEnumerable<ConsignmentItemR6ForInterventionType> ToCertexConsignmentItems(
        this CustomsConsignmentItem[] source
    )
    {
        return source.Select(ToCertexConsignmentItem);
    }

    internal static TracesNT.WebServices.InterventionMessageInformationType ToCertexInterventionType(this InterventionType source)
    {
        return source switch
        {
            InterventionType.ForceWriteOff => InterventionMessageInformationType.Item01,

            InterventionType.AmendWriteOff => InterventionMessageInformationType.Item02,

            InterventionType.DeleteWriteOff => InterventionMessageInformationType.Item03,

            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }

    private static TracesNT.WebServices.ConsignmentItemR6ForInterventionType ToCertexConsignmentItem(
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

    private static UniversalUnitOfMeasureType ToCertex(this UnitOfMeasureType source)
    {
        return Enum.Parse<UniversalUnitOfMeasureType>(source.ToString());
    }
}
