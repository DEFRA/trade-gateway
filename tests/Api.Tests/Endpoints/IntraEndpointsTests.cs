namespace Api.Tests.Endpoints;

public class IntraEndpointsTests(TradeGatewayWebApplicationFactory factory) : IClassFixture<TradeGatewayWebApplicationFactory>
{
    [Fact]
    public async Task OpenApi_VerifyAsExpected()
    {
        var client = factory.CreateClient();
        var response = await client.GetStringAsync("/intra/GB123", TestContext.Current.CancellationToken);

        await VerifyJson(response);
    }
}