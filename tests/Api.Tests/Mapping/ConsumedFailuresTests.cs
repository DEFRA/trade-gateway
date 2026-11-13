using Api.Mapping;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Tests.Mapping;

public class ConsumedFailuresTests
{
    [Theory]
    [InlineData("02", ConsumedFailure.RecordDoesNotExist)]
    [InlineData("03", ConsumedFailure.ReservationConsumed)]
    [InlineData("05", ConsumedFailure.ActiveReservationExists)]
    [InlineData("06", ConsumedFailure.WriteOffExists)]
    [InlineData("07", ConsumedFailure.WriteOffCannotBePerformed)]
    [InlineData(" 03 ", ConsumedFailure.ReservationConsumed)]
    [InlineData("01", ConsumedFailure.Unrecognised)]
    [InlineData("99", ConsumedFailure.Unrecognised)]
    [InlineData("", ConsumedFailure.Unrecognised)]
    [InlineData(null, ConsumedFailure.Unrecognised)]
    public void Decode_MapsOutcomeCode(string? outcome, ConsumedFailure expected)
    {
        ConsumedFailures.Decode(outcome).Should().Be(expected);
    }
}
