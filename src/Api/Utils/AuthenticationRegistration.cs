using Api.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
namespace Api.Utils;

[ExcludeFromCodeCoverage]
public static class AuthenticationRegistration
{
    public static void AddApiAuthentication(this WebApplicationBuilder builder)
    {
        var authSection = builder.Configuration.GetRequiredSection("Authentication");
        builder
            .Services.AddOptions<AuthenticationConfig>()
            .Bind(authSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var authConfig = authSection.Get<AuthenticationConfig>()!;

        builder
            .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authConfig.Authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false, // Cognito M2M access tokens (client_credentials) have no aud claim
                };
            });

        builder
            .Services.AddAuthorizationBuilder()
            .AddPolicy(
                "ApiAccess",
                policy => policy.RequireAuthenticatedUser().RequireClaim("scope", authConfig.Scope)
            );
    }
}
