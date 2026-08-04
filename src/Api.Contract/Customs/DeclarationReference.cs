#nullable enable
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>
/// The customs declaration an allocation was made against. Upstream this is a choice between an LRN
/// and an MRN, and both carry the same underlying string, so the discriminator is never inferable
/// from the value alone.
/// </summary>
public record DeclarationReference
{
    [JsonPropertyName("type")]
    [Description("Which kind of declaration reference this is: MRN or LRN.")]
    public required DeclarationReferenceType Type { get; init; }

    [JsonPropertyName("value")]
    [Description("The declaration reference itself.")]
    public required string Value { get; init; }
}

/// <remarks>
/// Members are PascalCase for Sonar S2342 and carry the wire spelling explicitly; the acronym form
/// is what consumers see.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<DeclarationReferenceType>))]
public enum DeclarationReferenceType
{
    /// <summary>Local Reference Number — pre-lodgement, assigned by the declarant.</summary>
    [JsonStringEnumMemberName("LRN")]
    Lrn,

    /// <summary>Movement Reference Number — assigned by customs on acceptance.</summary>
    [JsonStringEnumMemberName("MRN")]
    Mrn,
}
