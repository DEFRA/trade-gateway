using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

[JsonConverter(typeof(JsonStringEnumConverter<InterventionType>))]
public enum InterventionType
{
    DocumentCheck,
    IdentityCheck,
    PhysicalCheck,
}
