using Microsoft.Extensions.Options;
using TracesNT;

namespace Api.Utils;

public static class TracesNtRegistration
{
    /// <summary>
    /// Binds one <see cref="TracesNtCredentials"/> named-options instance per
    /// <see cref="TracesNtCredentialKeys"/> entry, from <c>TracesNt:Credentials:{Key}</c>.
    /// </summary>
    /// <remarks>
    /// Every key is validated on start by the <c>[Required]</c> attributes on
    /// <see cref="TracesNtCredentials"/>. A credential set that bound to empty strings would make
    /// <c>WsSecurityMessageInspector</c> omit the security header and call TracesNT
    /// unauthenticated, so refusing to boot is the only safe response to missing configuration.
    /// The resulting <c>OptionsValidationException.Message</c> names the failing property but not
    /// the credential set; <c>OptionsName</c> carries the key.
    /// </remarks>
    public static IServiceCollection AddTracesNtCredentials(
        this IServiceCollection services,
        IConfiguration tracesNtSection
    )
    {
        foreach (var credentialKey in TracesNtCredentialKeys.All)
        {
            services
                .AddOptions<TracesNtCredentials>(credentialKey)
                .Bind(tracesNtSection.GetSection($"Credentials:{credentialKey}"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        return services;
    }
}
