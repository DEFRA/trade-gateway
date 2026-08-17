#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TradeAddress
{
    [JsonPropertyName("postcodeCode")]
    [Description("unece:postcodeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? PostcodeCode { get; init; }

    [JsonPropertyName("lineOne")]
    public string? LineOne { get; init; }

    [JsonPropertyName("lineTwo")]
    public string? LineTwo { get; init; }

    [JsonPropertyName("cityName")]
    public string? CityName { get; init; }

    [JsonPropertyName("countryId")]
    [Description("unece:countryId is @vocab in unece-context-D23B.jsonld (e.g. ISO 3166-1 alpha-2 as a vocabulary-relative token).")]
    public string? CountryId { get; init; }

    [JsonPropertyName("countryName")]
    public string? CountryName { get; init; }

    [JsonPropertyName("countrySubDivisionName")]
    public string? CountrySubDivisionName { get; init; }
}
