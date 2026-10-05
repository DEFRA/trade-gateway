using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Api.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Utils;

/// <summary>
/// Development-only stand-ins for the two token issuers the app trusts, both signing with the same
/// local key and publishing it over OIDC discovery so the bearer schemes can verify what they mint.
/// Only the Cognito half speaks OIDC; STS uses the AWS Query protocol.
/// </summary>
[ExcludeFromCodeCoverage]
public static class LocalTokenServer
{
    private static readonly RSA Rsa = RSA.Create(2048);
    public static readonly RsaSecurityKey Key = new(Rsa) { KeyId = "local-dev" };
    private static readonly SigningCredentials Credentials = new(Key, SecurityAlgorithms.RsaSha256);

    public static void MapLocalTokenEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        var authConfig = app.Services.GetRequiredService<IOptions<AuthenticationConfig>>().Value;
        MapCognitoEndpoints(app, authConfig.Cognito.Authority);
        MapStsEndpoints(app, authConfig.Sts.Authority);
    }

    /// <summary>Discovery plus a <c>client_credentials</c>-style token endpoint.</summary>
    private static void MapCognitoEndpoints(WebApplication app, string authority)
    {
        var prefix = Prefix(authority);
        MapDiscoveryEndpoints(app, authority, prefix, hasTokenEndpoint: true);
        MapTokenEndpoint(app, authority, prefix);
    }

    /// <summary>
    /// Discovery (the <c>Sts</c> bearer scheme resolves signing keys through it) plus the
    /// <c>GetWebIdentityToken</c> stand-in the AWS SDK talks to. Deliberately no <c>/token</c>
    /// endpoint: real STS has no such operation, so callers exercise the same path the publisher does.
    /// </summary>
    private static void MapStsEndpoints(WebApplication app, string authority)
    {
        var prefix = Prefix(authority);
        MapDiscoveryEndpoints(app, authority, prefix, hasTokenEndpoint: false);
        MapGetWebIdentityTokenEndpoint(app, authority, prefix);
    }

    private static string Prefix(string authority) => new Uri(authority).AbsolutePath.TrimEnd('/');

    private static void MapDiscoveryEndpoints(
        WebApplication app,
        string authority,
        string prefix,
        bool hasTokenEndpoint
    )
    {
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
                },
            },
        };

        var metadata = new Dictionary<string, object>
        {
            ["issuer"] = authority,
            ["jwks_uri"] = $"{authority}/.well-known/jwks",
        };

        if (hasTokenEndpoint)
        {
            metadata["token_endpoint"] = $"{authority}/token";
            metadata["grant_types_supported"] = new[] { "client_credentials" };
        }

        app.MapGet($"{prefix}/.well-known/openid-configuration", () => Results.Json(metadata))
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapGet($"{prefix}/.well-known/jwks", () => Results.Json(jwks)).AllowAnonymous().ExcludeFromDescription();
    }

    private static void MapTokenEndpoint(WebApplication app, string authority, string prefix) =>
        app.MapPost(
                $"{prefix}/token",
                ([FromForm] string? scope, [FromForm] string? audience, [FromForm] string? sub) =>
                {
                    var claims = new Dictionary<string, object> { ["scope"] = scope ?? "" };
                    if (!string.IsNullOrEmpty(sub))
                        claims["sub"] = sub;

                    var descriptor = new SecurityTokenDescriptor
                    {
                        Issuer = authority,
                        Audience = audience,
                        Claims = claims,
                        Expires = DateTime.UtcNow.AddHours(1),
                        SigningCredentials = Credentials,
                    };
                    var token = new JwtSecurityTokenHandler().CreateToken(descriptor);
                    return Results.Json(
                        new
                        {
                            access_token = new JwtSecurityTokenHandler().WriteToken(token),
                            token_type = "Bearer",
                            expires_in = 3600,
                        }
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .ExcludeFromDescription();

    /// <summary>
    /// Stands in for <c>sts:GetWebIdentityToken</c>, which localstack does not implement. A local
    /// caller points <c>AWS_ENDPOINT_URL_STS</c> at the Sts authority and gets back a JWT signed
    /// with the same key the JWKS endpoint publishes, wrapped in AWS's Query-protocol XML envelope.
    /// Mounted on the prefix itself: the SDK posts to the endpoint URL, with no operation path.
    /// </summary>
    private static void MapGetWebIdentityTokenEndpoint(WebApplication app, string authority, string prefix) =>
        // AWS Query protocol: Action=GetWebIdentityToken&Audience.member.1=trade-gateway&DurationSeconds=900&SigningAlgorithm=RS256
        app.MapPost(
                prefix,
                async (HttpRequest request) =>
                {
                    var form = await request.ReadFormAsync();
                    if (form["Action"] != "GetWebIdentityToken")
                        return StsError(400, "InvalidAction", $"Unsupported action '{form["Action"]}'");

                    var caller = AccessKeyOf(request);
                    if (caller is null)
                        return StsError(
                            403,
                            "MissingAuthenticationToken",
                            "Request must be signed with AWS credentials"
                        );

                    var expires = DateTime.UtcNow.AddSeconds(
                        int.TryParse(form["DurationSeconds"], out var seconds) ? seconds : 900
                    );

                    var descriptor = new SecurityTokenDescriptor
                    {
                        Issuer = authority,
                        Audience = form["Audience.member.1"].ToString(),
                        Claims = new Dictionary<string, object> { ["sub"] = caller },
                        Expires = expires,
                        SigningCredentials = Credentials,
                    };
                    var handler = new JwtSecurityTokenHandler();

                    return Results.Text(
                        $"""<GetWebIdentityTokenResponse xmlns="https://sts.amazonaws.com/doc/2011-06-15/"><GetWebIdentityTokenResult><WebIdentityToken>{handler.WriteToken(handler.CreateToken(descriptor))}</WebIdentityToken><Expiration>{expires:yyyy-MM-ddTHH:mm:ss}Z</Expiration></GetWebIdentityTokenResult></GetWebIdentityTokenResponse>""",
                        "text/xml"
                    );
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .ExcludeFromDescription();

    /// <summary>
    /// Real STS issues the token to the caller's IAM role, and the gateway's own authorization decides
    /// what that role may do. Locally every caller signs with made-up AWS credentials, so the access key
    /// stands in for the role: the publisher signs as <c>trade-gateway-publisher</c>, the journey tests
    /// as <c>trade-gateway-journey-tests</c>. A key no principal maps to still gets a token, and is
    /// refused by authorization, as an unmapped role would be.
    /// </summary>
    private static string? AccessKeyOf(HttpRequest request)
    {
        // SigV4: "AWS4-HMAC-SHA256 Credential=<access key>/<date>/<region>/sts/aws4_request, ..."
        var header = request.Headers.Authorization.ToString();
        var start = header.IndexOf("Credential=", StringComparison.Ordinal);
        if (start < 0)
            return null;

        start += "Credential=".Length;
        var end = header.IndexOf('/', start);
        return end > start ? header[start..end] : null;
    }

    private static IResult StsError(int statusCode, string code, string message) =>
        Results.Text(
            $"""<ErrorResponse xmlns="https://sts.amazonaws.com/doc/2011-06-15/"><Error><Type>Sender</Type><Code>{code}</Code><Message>{message}</Message></Error></ErrorResponse>""",
            "text/xml",
            statusCode: statusCode
        );
}
