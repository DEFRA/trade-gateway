using Api.Config;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace Api.Utils;

[ExcludeFromCodeCoverage]
public static class LocalOidcServer
{
    private static readonly RSA Rsa = RSA.Create(2048);
    public static readonly RsaSecurityKey Key = new(Rsa) { KeyId = "local-dev" };
    private static readonly SigningCredentials Credentials = new(Key, SecurityAlgorithms.RsaSha256);

    public static void MapLocalOidcEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        var authConfig = app.Services.GetRequiredService<IOptions<AuthenticationConfig>>().Value;
        MapIssuerEndpoints(app, authConfig.Cognito.Authority);
        MapIssuerEndpoints(app, authConfig.Sts.Authority);
    }

    private static void MapIssuerEndpoints(WebApplication app, string authority)
    {
        var prefix = new Uri(authority).AbsolutePath.TrimEnd('/');
        var rsaParams = Rsa.ExportParameters(false);

        var jwks = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = Key.KeyId,
                    alg = "RS256",
                    n = Base64UrlEncoder.Encode(rsaParams.Modulus!),
                    e = Base64UrlEncoder.Encode(rsaParams.Exponent!),
                }
            }
        };

        app.MapGet($"{prefix}/.well-known/openid-configuration", () => Results.Json(new
        {
            issuer = authority,
            jwks_uri = $"{authority}/.well-known/jwks",
            token_endpoint = $"{authority}/token",
            grant_types_supported = new[] { "client_credentials" },
        })).AllowAnonymous();

        app.MapGet($"{prefix}/.well-known/jwks", () => Results.Json(jwks))
           .AllowAnonymous();

        app.MapPost($"{prefix}/token", ([FromForm] string? scope) =>
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = authority,
                Claims = new Dictionary<string, object> { ["scope"] = scope ?? "" },
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = Credentials,
            };
            var token = new JwtSecurityTokenHandler().CreateToken(descriptor);
            return Results.Json(new
            {
                access_token = new JwtSecurityTokenHandler().WriteToken(token),
                token_type = "Bearer",
                expires_in = 3600,
            });
        }).AllowAnonymous().DisableAntiforgery();
    }
}
