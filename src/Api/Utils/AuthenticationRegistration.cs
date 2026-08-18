using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Api.Authorization;
using Api.Config;
using Api.Utils.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Utils;

[ExcludeFromCodeCoverage]
public static class AuthenticationRegistration
{
    private const string CognitoScheme = "Cognito";
    private const string StsScheme = "Sts";
    private const string MultiScheme = "MultiIssuer";

    public static void AddApiAuthentication(this WebApplicationBuilder builder)
    {
        var authConfig = builder.BindConfig<AuthenticationConfig>("Authentication");

        builder
            .Services.AddAuthentication(MultiScheme)
            .AddIssuerRouter(authConfig.Sts.Authority)
            .AddIssuerBearer(CognitoScheme, authConfig.Cognito, builder.Environment.IsDevelopment())
            .AddIssuerBearer(StsScheme, authConfig.Sts, builder.Environment.IsDevelopment());

        builder.Services.ConfigureProxyBackchannel(CognitoScheme);
        builder.Services.ConfigureProxyBackchannel(StsScheme);

        builder.BindConfig<AuthorizationConfig>("Authorization");
        builder.Services.AddSingleton<IValidateOptions<AuthorizationConfig>, AuthorizationConfigValidator>();
        builder.Services.AddSingleton<IResourceAuthorizer, ResourceAuthorizer>();
        builder.Services.AddSingleton<IAuthorizationHandler, ResourcePermissionHandler>();

        builder.Services.AddApiAuthorization(authConfig.Cognito);
    }

    private static T BindConfig<T>(this WebApplicationBuilder builder, string section)
        where T : class
    {
        builder.Services.AddOptions<T>().BindConfiguration(section).ValidateDataAnnotations().ValidateOnStart();
        return builder.Configuration.GetRequiredSection(section).Get<T>()!;
    }

    private static AuthenticationBuilder AddIssuerRouter(this AuthenticationBuilder auth, string stsAuthority) =>
        auth.AddPolicyScheme(
            MultiScheme,
            null,
            opts =>
                opts.ForwardDefaultSelector = ctx =>
                {
                    var parts = ctx.Request.Headers.Authorization.FirstOrDefault()?.Split(' ');
                    var token = parts is { Length: > 0 } ? parts[^1] : null;
                    if (token != null)
                    {
                        var handler = new JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token) && handler.ReadJwtToken(token).Issuer == stsAuthority)
                            return StsScheme;
                    }
                    return CognitoScheme;
                }
        );

    private static AuthenticationBuilder AddIssuerBearer(
        this AuthenticationBuilder auth,
        string scheme,
        IssuerAuthenticationConfig config,
        bool isDev
    ) =>
        auth.AddJwtBearer(
            scheme,
            opts =>
            {
                opts.Authority = config.Authority;
                opts.RequireHttpsMetadata = config.Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = config.Authority,
                    ValidateIssuer = !isDev,
                    ValidAudience = config.Audience,
                    ValidateAudience = config.Audience is not null,
                    AuthenticationType = scheme, // required so the ClaimsIdentity carries the scheme name for policy checks
                };
            }
        );

    private static void ConfigureProxyBackchannel(this IServiceCollection services, string scheme) =>
        services
            .AddOptions<JwtBearerOptions>(scheme)
            .Configure<ProxyHttpMessageHandler>((opts, proxy) => opts.BackchannelHttpHandler = proxy);

    private static void AddApiAuthorization(this IServiceCollection services, IssuerAuthenticationConfig cognitoConfig)
    {
        ArgumentNullException.ThrowIfNull(cognitoConfig.Scope);

        // ApiAccess: authenticated principal with the right scheme + scope.
        var apiAccess = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx =>
            {
                var scheme = ctx.User.Identities.FirstOrDefault(i => i.IsAuthenticated)?.AuthenticationType;
                var scopes = (ctx.User.FindFirst("scope")?.Value ?? "").Split(' ');

                return (scheme == CognitoScheme && scopes.Contains(cognitoConfig.Scope)) || scheme == StsScheme; // STS tokens carry no scope claim — issuer/signature validation is sufficient
            })
            .Build();

        // Fallback applied to every endpoint without explicit auth metadata: ApiAccess first,
        // then fine-grained per-principal resource/action authorisation (ADR-0005).
        var fallback = new AuthorizationPolicyBuilder()
            .Combine(apiAccess)
            .AddRequirements(new ResourcePermissionRequirement())
            .Build();

        services.AddAuthorizationBuilder().AddPolicy("ApiAccess", apiAccess).SetFallbackPolicy(fallback);
    }
}
