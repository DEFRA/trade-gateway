using Api.Authorization;
using Api.Config;
using AwesomeAssertions;

namespace Api.Tests.Authorization;

public class AuthorizationConfigValidatorTests
{
    private static readonly AuthorizationConfigValidator Validator = new();

    [Fact]
    public void Valid_config_passes()
    {
        var config = new AuthorizationConfig
        {
            Principals = new Dictionary<string, string> { ["intra-reader"] = "sub-1" },
            Permissions = new Dictionary<string, List<PermissionGrant>>
            {
                ["intra-reader"] = [new PermissionGrant { Actions = ["READ"], Resource = "/certificates/intras/**" }],
            },
        };

        Validator.Validate(null, config).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Alias_without_principal_fails()
    {
        var config = new AuthorizationConfig
        {
            Principals = new Dictionary<string, string>(),
            Permissions = new Dictionary<string, List<PermissionGrant>>
            {
                ["intra-reader"] = [new PermissionGrant { Actions = ["READ"], Resource = "/certificates/intras/**" }],
            },
        };

        var result = Validator.Validate(null, config);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("intra-reader");
    }

    [Fact]
    public void Empty_permissions_list_fails()
    {
        var config = new AuthorizationConfig
        {
            Principals = new Dictionary<string, string> { ["intra-reader"] = "sub-1" },
            Permissions = new Dictionary<string, List<PermissionGrant>> { ["intra-reader"] = [] },
        };

        Validator.Validate(null, config).Failed.Should().BeTrue();
    }
}
