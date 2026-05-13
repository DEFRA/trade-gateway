namespace Api.Tests.OpenApi;

[Collection(IntegrationTestCollection.Name)]
public class OpenApiTests(TradeGatewayWebApplicationFactory factory)
{
    [Fact]
    public async Task OpenApi_VerifyAsExpected()
    {
        var client = factory.CreateClient();
        var response = await client.GetStringAsync("/.well-known/open-api.json", TestContext.Current.CancellationToken);

        await VerifyJson(response);
    }
}