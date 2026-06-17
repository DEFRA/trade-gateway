using System.Net;
using System.Net.Http.Headers;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class AuthSchemesTests(TradeGatewayWebApplicationFactory factory)
{
    private const string CognitoScope = "trade-gateway-resource-srv/access";

    [Fact]
    public async Task AuthTest_WithCognitoToken_Returns200()
    {
        var client = factory.CreateClientWithToken(await factory.GetCognitoTokenAsync(CognitoScope));
        var response = await client.GetAsync("/auth-test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthTest_WithStsToken_Returns200()
    {
        var client = factory.CreateClientWithToken(await factory.GetStsTokenAsync("trade-gateway"));
        var response = await client.GetAsync("/auth-test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthTest_WithStsTokenWrongAudience_Returns401()
    {
        var client = factory.CreateClientWithToken(await factory.GetStsTokenAsync("other-service"));
        var response = await client.GetAsync("/auth-test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthTest_WithNoToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/auth-test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
