using System.Text.Json;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract;

namespace Api.Mapping;

internal static class SpsPartyMapper
{
    internal static TradeParty? Map(SPSPartyType? source)
    {
        if (source is null)
            return null;

        return new TradeParty
        {
            Identifier = source.ID?.Value,
            Name = source.Name?.Value,
            PartyRoleCode = source.RoleCode?.Value.XmlEnumCode(),
            PartyTypeCode = source.TypeCode?.FirstOrDefault() is { Value: { } v }
                ? JsonSerializer.SerializeToElement(v)
                : (JsonElement?)null,
            PostalAddress = SpsAddressMapper.Map(source.SpecifiedSPSAddress),
            DefinedContact = source.SpecifiedSPSPerson?.Name?.Value is { } name
                ? [new TradePartyDefinedContactItem { PersonName = name }]
                : null,
        };
    }
}
