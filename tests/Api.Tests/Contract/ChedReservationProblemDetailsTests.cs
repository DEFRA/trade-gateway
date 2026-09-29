using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Tests.Contract;

public class ChedReservationProblemDetailsTests
{
    private static readonly JsonSerializerOptions s_options = JsonSerializerOptions.Web;

    private static ChedReservationProblemDetails Deserialize(string json) =>
        JsonSerializer.Deserialize<ChedReservationProblemDetails>(json, s_options)!;

    [Fact]
    public void Deserialize_ReadsTheStandardProblemFields()
    {
        var problem = Deserialize(
            """
            {
              "title": "Quantity management request not executed",
              "detail": "TracesNT refused the reservation.",
              "status": 409
            }
            """
        );

        problem.Title.Should().Be("Quantity management request not executed");
        problem.Detail.Should().Be("TracesNT refused the reservation.");
        problem.Status.Should().Be(409);
    }

    [Fact]
    public void Deserialize_KeepsUnmappedMembersAsExtensions()
    {
        var problem = Deserialize("""{ "chedId": "CHEDA.GB.2026.0000001", "mrn": "26GB0000000000001" }""");

        problem.Extensions.Should().ContainKeys("chedId", "mrn");
        ((JsonElement)problem.Extensions!["chedId"]).GetString().Should().Be("CHEDA.GB.2026.0000001");
    }

    [Theory]
    [InlineData("Unrecognised", ReservationFailureReason.Unrecognised)]
    [InlineData("QuantitiesInsufficient", ReservationFailureReason.QuantitiesInsufficient)]
    [InlineData("WriteOffExists", ReservationFailureReason.WriteOffExists)]
    public void Reason_WhenAKnownName_ParsesIt(string name, ReservationFailureReason expected)
    {
        var problem = Deserialize($$"""{ "reason": "{{name}}" }""");

        problem.Reason.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{ "status": 409 }""")]
    [InlineData("""{ "reason": null }""")]
    [InlineData("""{ "reason": "NotARealReason" }""")]
    [InlineData("""{ "reason": "" }""")]
    [InlineData("""{ "reason": 5 }""")]
    [InlineData("""{ "reason": { "code": "05" } }""")]
    public void Reason_WhenAbsentOrNotARecognisedString_IsNull(string json)
    {
        var problem = Deserialize(json);

        problem.Reason.Should().BeNull();
    }

    [Fact]
    public void Serialize_WritesReasonOnceFromExtensions()
    {
        var problem = Deserialize("""{ "reason": "CnCodesMismatch" }""");

        var json = JsonSerializer.Serialize(problem, s_options);

        using var document = JsonDocument.Parse(json);
        var reasons = document.RootElement.EnumerateObject().Where(p => p.NameEquals("reason")).ToList();
        reasons.Should().ContainSingle().Which.Value.GetString().Should().Be("CnCodesMismatch");
        json.Should().NotContain("computedReason");
    }

    [Fact]
    public void Reason_WhenExtensionsIsNull_IsNull()
    {
        var problem = new ChedReservationProblemDetails { Extensions = null };

        problem.Reason.Should().BeNull();
    }

    [Fact]
    public void Reason_WhenExtensionHoldsAClrString_ParsesIt()
    {
        var problem = new ChedReservationProblemDetails
        {
            Extensions = new Dictionary<string, object> { ["reason"] = "LineNumbersMismatch" },
        };

        problem.Reason.Should().Be(ReservationFailureReason.LineNumbersMismatch);
    }

    [Fact]
    public void Reason_WhenExtensionHoldsANonStringClrValue_IsNull()
    {
        var problem = new ChedReservationProblemDetails
        {
            Extensions = new Dictionary<string, object> { ["reason"] = ReservationFailureReason.WriteOffExists },
        };

        problem.Reason.Should().BeNull();
    }

    /// <summary>
    /// Mirrors the body <c>CustomsChedQuantityEndpoints</c> builds with <c>Results.Problem</c>: the
    /// enum is written by its <c>JsonStringEnumConverter</c> and must read back as the same value.
    /// </summary>
    [Fact]
    public void RoundTrip_FromTheServerProblemShape_RecoversEveryField()
    {
        var server = new ProblemDetails
        {
            Title = "Quantity management request not executed",
            Detail = "TracesNT refused the reservation of CHED 'CHEDA.GB.2026.0000001'.",
            Status = StatusCodes.Status409Conflict,
            Extensions =
            {
                ["chedId"] = "CHEDA.GB.2026.0000001",
                ["mrn"] = "26GB0000000000001",
                ["reason"] = ReservationFailureReason.QuantitiesInsufficient,
                ["chedStatus"] = null,
            },
        };

        var problem = Deserialize(JsonSerializer.Serialize(server, s_options));

        problem.Title.Should().Be(server.Title);
        problem.Detail.Should().Be(server.Detail);
        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Reason.Should().Be(ReservationFailureReason.QuantitiesInsufficient);
    }
}
