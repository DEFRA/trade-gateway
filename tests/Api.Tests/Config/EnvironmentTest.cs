using Microsoft.AspNetCore.Builder;

namespace Api.Tests.Config;

public class EnvironmentTest
{
    [Fact]
    public void IsNotDevModeByDefault()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var isDev = Api.Config.Environment.IsDevMode(builder);
        Assert.False(isDev);
    }
}
