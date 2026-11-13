#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LogisticsPackage
{
    [JsonPropertyName("levelCode")]
    [Description("Packaging hierarchy level per the UNECE PackagingLevelCode list. For example 4, no packaging hierarchy, for a single flat set of packages.")]
    public int? LevelCode { get; init; }

    [JsonPropertyName("typeCode")]
    [Description("Package type per the UNECE PackageTypeCode list (UN/EDIFACT Recommendation 21), for example a cage or crate. Held as an open string. unece:typeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("itemQuantity")]
    [Description("The number of packages of this type, the package count.")]
    public int? ItemQuantity { get; init; }
}
