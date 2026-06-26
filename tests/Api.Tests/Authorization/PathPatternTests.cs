using Api.Authorization;
using AwesomeAssertions;

namespace Api.Tests.Authorization;

public class PathPatternTests
{
    [Theory]
    // Exact literal match
    [InlineData("/certificates/intras", "/certificates/intras", true)]
    [InlineData("/certificates/intras", "/certificates/intras/ABC123", false)]
    [InlineData("/certificates/intras/ABC123", "/certificates/intras/ABC123", true)]
    [InlineData("/certificates/intras/ABC123", "/certificates/intras/OTHER", false)]
    // Single-segment wildcard: instance access without collection access
    [InlineData("/certificates/intras/*", "/certificates/intras/ABC123", true)]
    [InlineData("/certificates/intras/*", "/certificates/intras", false)]
    [InlineData("/certificates/intras/*", "/certificates/intras/ABC123/detail", false)]
    // ADR examples for single-segment wildcard
    [InlineData("/certificates/*/detail", "/certificates/cheds/detail", true)]
    [InlineData("/certificates/*/detail", "/certificates/cheds/123/detail", false)]
    // ** suffix: zero or more segments
    [InlineData("/certificates/**", "/certificates", true)]
    [InlineData("/certificates/**", "/certificates/", true)]
    [InlineData("/certificates/**", "/certificates/cheds", true)]
    [InlineData("/certificates/**", "/certificates/cheds/123", true)]
    [InlineData("/certificates/intras/**", "/certificates/intras", true)]
    [InlineData("/certificates/intras/**", "/certificates/intras/ABC123", true)]
    [InlineData("/certificates/**", "/reference-data/x", false)]
    // Case-insensitivity
    [InlineData("/certificates/intras/**", "/Certificates/Intras/ABC123", true)]
    [InlineData("/Reference-Data/**", "/reference-data/metadata/x", true)]
    // Trailing-slash normalisation
    [InlineData("/certificates/intras/", "/certificates/intras", true)]
    [InlineData("/certificates/intras", "/certificates/intras/", true)]
    public void Matches(string pattern, string path, bool expected) =>
        PathPattern.Matches(pattern, path).Should().Be(expected);
}
