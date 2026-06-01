#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record CargoNature
{
    [JsonPropertyName("typeCode")]
    [Description("unece:typeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? TypeCode { get; init; }
}
