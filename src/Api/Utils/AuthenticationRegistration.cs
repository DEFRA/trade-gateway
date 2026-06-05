using Api.Config;
using Api.Utils.Http;
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
                    ValidIssuer      = authConfig.Authority,
                    ValidateAudience = false, // Cognito M2M access tokens (client_credentials) have no aud claim
                };
            });

        builder.Services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<ProxyHttpMessageHandler>((options, proxy) => options.BackchannelHttpHandler = proxy);

        builder
            .Services.AddAuthorizationBuilder()
            .AddPolicy(
                "ApiAccess",
                policy => policy.RequireAuthenticatedUser().RequireClaim("scope", authConfig.Scope)
            );
    }
}
