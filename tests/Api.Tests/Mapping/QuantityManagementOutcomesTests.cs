using Api.Mapping;
using Microsoft.AspNetCore.Http;

namespace Api.Tests.Mapping;

public class QuantityManagementOutcomesTests
{
    [Theory]
    [InlineData(QuantityManagementOutcomes.Executed, true)]
    [InlineData(QuantityManagementOutcomes.ExecutedWithStatusWarning, true)]
    [InlineData(QuantityManagementOutcomes.RecordDoesNotExist, false)]
    [InlineData(QuantityManagementOutcomes.AlreadyConsumed, false)]
    [InlineData(QuantityManagementOutcomes.ActiveReservationExists, false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("99", false)]
    public void IsSuccess_returns_expected_result(string? outcome, bool expected)
    {
        var result = QuantityManagementOutcomes.IsSuccess(outcome);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(QuantityManagementOutcomes.Executed, "Request successfully executed.")]
    [InlineData(
        QuantityManagementOutcomes.RecordDoesNotExist,
        "Request was not executed - no record exists for this MRN and CHED."
    )]
    [InlineData(
        QuantityManagementOutcomes.AlreadyConsumed,
        "Request was not executed - the reservation has been consumed."
    )]
    [InlineData(
        QuantityManagementOutcomes.ExecutedWithStatusWarning,
        "Request executed, but the CHED status changed during the clearance process. "
            + "The reserved quantities were still written off."
    )]
    [InlineData(
        QuantityManagementOutcomes.ActiveReservationExists,
        "Request was not executed - an active reservation exists for this MRN and CHED."
    )]
    public void Describe_returns_expected_description_for_known_outcome(string outcome, string expected)
    {
        var result = QuantityManagementOutcomes.Describe(outcome);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Describe_returns_expected_description_when_outcome_is_null()
    {
        var result = QuantityManagementOutcomes.Describe(null);

        Assert.Equal("TracesNT returned no quantity management outcome.", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("99")]
    [InlineData("UNKNOWN")]
    public void Describe_returns_expected_description_for_unknown_outcome(string outcome)
    {
        var result = QuantityManagementOutcomes.Describe(outcome);

        Assert.Equal($"Unrecognised quantity management outcome '{outcome}'.", result);
    }

    [Theory]
    [InlineData(QuantityManagementOutcomes.Executed, StatusCodes.Status200OK)]
    [InlineData(QuantityManagementOutcomes.ExecutedWithStatusWarning, StatusCodes.Status200OK)]
    [InlineData(QuantityManagementOutcomes.RecordDoesNotExist, StatusCodes.Status404NotFound)]
    [InlineData(QuantityManagementOutcomes.AlreadyConsumed, StatusCodes.Status409Conflict)]
    [InlineData(QuantityManagementOutcomes.ActiveReservationExists, StatusCodes.Status409Conflict)]
    [InlineData(null, StatusCodes.Status502BadGateway)]
    [InlineData("", StatusCodes.Status502BadGateway)]
    [InlineData("99", StatusCodes.Status502BadGateway)]
    public void ToStatusCode_returns_expected_status_code(string? outcome, int expected)
    {
        var result = QuantityManagementOutcomes.ToStatusCode(outcome);

        Assert.Equal(expected, result);
    }
}
