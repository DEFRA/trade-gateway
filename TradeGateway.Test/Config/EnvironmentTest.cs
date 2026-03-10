using Microsoft.AspNetCore.Builder;

namespace TradeGateway.Test.Config;

public class EnvironmentTest
{
    [Fact]
    public void IsNotDevModeByDefault()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var isDev = TradeGateway.Config.Environment.IsDevMode(builder);
        Assert.False(isDev);
    }
}