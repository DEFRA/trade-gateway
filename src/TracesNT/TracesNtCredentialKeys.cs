namespace TracesNT;

/// <summary>
/// The closed set of TracesNT credential sets. Each key names both a
/// <see cref="TracesNtCredentials"/> named-options instance and a <c>TracesNt:Credentials:{Key}</c>
/// configuration section.
/// </summary>
/// <remarks>
/// Every key in <see cref="All"/> is validated at startup, so adding one here without adding the
/// matching configuration will stop the host from booting. That is deliberate: a credential set
/// that resolves to empty strings would make <c>WsSecurityMessageInspector</c> omit the security
/// header altogether and call TracesNT unauthenticated.
/// </remarks>
public static class TracesNtCredentialKeys
{
    /// <summary>The original gateway account, used by every certificate and reference-data port.</summary>
    public const string Default = "Default";

    /// <summary>The customs account, used by the quantity-management port.</summary>
    public const string Customs = "Customs";

    public static readonly string[] All = [Default, Customs];
}
