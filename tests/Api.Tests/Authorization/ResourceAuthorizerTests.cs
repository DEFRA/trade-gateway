using Api.Authorization;
using Api.Config;
using AwesomeAssertions;
using Microsoft.Extensions.Options;

namespace Api.Tests.Authorization;

public class ResourceAuthorizerTests
{
    private const string IntraSub = "sub-intra";
    private const string RefDataSub = "sub-ref";

    private static ResourceAuthorizer Build() =>
        new(
            Options.Create(
                new AuthorizationConfig
                {
                    Principals = new Dictionary<string, string>
                    {
                        ["intra-reader"] = IntraSub,
                        ["reference-data-reader"] = RefDataSub,
                    },
                    Permissions = new Dictionary<string, List<PermissionGrant>>
                    {
                        ["intra-reader"] =
                        [
                            new PermissionGrant { Actions = ["READ", "WRITE"], Resource = "/certificates/intras/**" },
                        ],
                        ["reference-data-reader"] =
                        [
                            new PermissionGrant { Actions = ["READ"], Resource = "/reference-data/**" },
                        ],
                    },
                }
            )
        );

    [Fact]
    public void Allows_read_on_granted_resource() =>
        Build().IsAuthorized(IntraSub, "/certificates/intras/ABC123", "GET").Should().BeTrue();

    [Fact]
    public void Denies_read_on_other_principals_resource() =>
        Build().IsAuthorized(IntraSub, "/reference-data/metadata/x", "GET").Should().BeFalse();

    [Fact]
    public void Allows_write_when_granted() =>
        Build().IsAuthorized(IntraSub, "/certificates/intras/ABC123", "POST").Should().BeTrue();

    [Fact]
    public void Maps_put_to_write() =>
        Build().IsAuthorized(IntraSub, "/certificates/intras/ABC123", "PUT").Should().BeTrue();

    [Fact]
    public void Denies_write_when_only_read_granted() =>
        Build().IsAuthorized(RefDataSub, "/reference-data/metadata/x", "POST").Should().BeFalse();

    [Theory]
    [InlineData("PATCH")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public void Denies_unmapped_methods(string method) =>
        Build().IsAuthorized(IntraSub, "/certificates/intras/ABC123", method).Should().BeFalse();

    [Fact]
    public void Denies_unknown_sub() =>
        Build().IsAuthorized("nobody", "/certificates/intras/ABC123", "GET").Should().BeFalse();

    [Fact]
    public void Denies_null_sub() =>
        Build().IsAuthorized(null, "/certificates/intras/ABC123", "GET").Should().BeFalse();

    [Fact]
    public void Method_resolution_is_case_insensitive() =>
        Build().IsAuthorized(IntraSub, "/certificates/intras/ABC123", "get").Should().BeTrue();
}
