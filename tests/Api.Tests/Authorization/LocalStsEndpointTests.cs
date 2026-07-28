using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace Api.Tests.Authorization;

/// <summary>
/// The local <c>GetWebIdentityToken</c> stand-in exists so a developer can point
/// <c>AWS_ENDPOINT_URL_STS</c> at this app instead of localstack, which does not implement the
/// operation. These tests drive it through the real AWS SDK client, so the hand-written XML
/// envelope cannot drift out of shape with the SDK's unmarshaller unnoticed.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class LocalStsEndpointTests(TradeGatewayWebApplicationFactory factory)
{
    private const string Audience = "trade-gateway";
    private const string StsAuthority = "http://localhost:5000/local/sts";

    [Fact]
    public async Task GetWebIdentityToken_IsParsedByTheAwsSdk()
    {
        var response = await factory.GetWebIdentityTokenAsync(Audience);

        Assert.False(string.IsNullOrEmpty(response.WebIdentityToken));
        Assert.NotNull(response.Expiration);
    }

    [Fact]
    public async Task GetWebIdentityToken_IssuesATokenTheStsSchemeAccepts()
    {
        var response = await factory.GetWebIdentityTokenAsync(Audience);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.WebIdentityToken);

        // The multi-issuer router forwards on `iss`, and the Sts scheme validates `aud`.
        Assert.Equal(StsAuthority, token.Issuer);
        Assert.Contains(Audience, token.Audiences);
        Assert.Equal("trade-gateway-publisher", token.Subject);
    }

    [Fact]
    public async Task GetWebIdentityToken_ReturnsATokenAuthorizedForTheCertificateEndpoints()
    {
        var response = await factory.GetWebIdentityTokenAsync(Audience);
        var client = factory.CreateClientWithToken(response.WebIdentityToken);

        // Reaching model validation means both authentication and the ADR-0005 resource check passed.
        var result = await client.GetAsync("/certificates/cheds", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
