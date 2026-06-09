using Api.Utils.Http;
using AwesomeAssertions;

namespace Api.Tests.Config;

public class AcceptLanguageParserTests
{
    [Theory]
    [InlineData(null, "en")]
    [InlineData("", "en")]
    [InlineData("   ", "en")]
    [InlineData("en", "en")]
    [InlineData("en-GB", "en")]
    [InlineData("cy-GB,cy;q=0.9,en;q=0.8", "cy")]
    [InlineData("fr;q=0.8,en;q=0.7", "fr")]
    public void GetPrimaryLanguageCode_ReturnsExpectedLanguage(string? acceptLanguage, string expected)
    {
        AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage).Should().Be(expected);
    }
}
