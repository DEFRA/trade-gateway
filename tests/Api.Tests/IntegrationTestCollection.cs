namespace Api.Tests;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<TradeGatewayWebApplicationFactory>
{
    public const string Name = "Integration";
}
