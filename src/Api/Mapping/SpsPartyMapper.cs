using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

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
            PartyRoleCode = MapRoleCode(source.RoleCode),
            PartyTypeCode = source.TypeCode
                ?.Where(c => !string.IsNullOrEmpty(c.Value))
                .Select(MapTypeCode)
                .ToList()
                .NullIfEmpty(),
            PostalAddress = SpsAddressMapper.Map(source.SpecifiedSPSAddress),
            DefinedContact = source.SpecifiedSPSPerson?.Name?.Value is { } name
                ? [new TradePartyDefinedContactItem { PersonName = name }]
                : null,
        };
    }

    static CodedValue? MapRoleCode(PartyRoleCodeType? roleCode) =>
        roleCode is null
            ? null
            : new CodedValue
            {
                Value = roleCode.Value.XmlEnumCode(),
                Name = roleCode.name,
                UrlId = roleCode.listID.ToCodelistUri(),
            };

    static CodedValue MapTypeCode(CodeType typeCode) =>
        new()
        {
            Value = typeCode.Value,
            Name = typeCode.name,
            UrlId = typeCode.listID.ToCodelistUri(),
        };
}
