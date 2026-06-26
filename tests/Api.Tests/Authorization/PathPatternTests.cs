using Api.Authorization;
using AwesomeAssertions;

namespace Api.Tests.Authorization;

public class PathPatternTests
{
    [Theory]
    // Exact literal match
    [InlineData("/certificates/intra", "/certificates/intra", true)]
    [InlineData("/certificates/intra", "/certificates/intra/ABC123", false)]
    [InlineData("/certificates/intra/ABC123", "/certificates/intra/ABC123", true)]
    [InlineData("/certificates/intra/ABC123", "/certificates/intra/OTHER", false)]
    // Single-segment wildcard: instance access without collection access
    [InlineData("/certificates/intra/*", "/certificates/intra/ABC123", true)]
    [InlineData("/certificates/intra/*", "/certificates/intra", false)]
    [InlineData("/certificates/intra/*", "/certificates/intra/ABC123/detail", false)]
    // ADR examples for single-segment wildcard
    [InlineData("/certificates/*/detail", "/certificates/ched/detail", true)]
    [InlineData("/certificates/*/detail", "/certificates/ched/123/detail", false)]
    // ** suffix: zero or more segments
    [InlineData("/certificates/**", "/certificates", true)]
    [InlineData("/certificates/**", "/certificates/", true)]
    [InlineData("/certificates/**", "/certificates/ched", true)]
    [InlineData("/certificates/**", "/certificates/ched/123", true)]
    [InlineData("/certificates/intra/**", "/certificates/intra", true)]
    [InlineData("/certificates/intra/**", "/certificates/intra/ABC123", true)]
    [InlineData("/certificates/**", "/reference-data/x", false)]
    // Case-insensitivity
    [InlineData("/certificates/intra/**", "/Certificates/Intra/ABC123", true)]
    [InlineData("/Reference-Data/**", "/reference-data/metadata/x", true)]
    // Trailing-slash normalisation
    [InlineData("/certificates/intra/", "/certificates/intra", true)]
    [InlineData("/certificates/intra", "/certificates/intra/", true)]
    public void Matches(string pattern, string path, bool expected) =>
        PathPattern.Matches(pattern, path).Should().Be(expected);
}
