using Microsoft.AspNetCore.Routing;
using Api.Filters;
using Api.Models;
using AwesomeAssertions;

namespace Api.Tests.Filters;

public class DataAnnotationsRouteValidatorTests
{
    [Fact]
    public void ReturnsErrors_When_RouteValuesMissing()
    {
        var routeValues = new RouteValueDictionary();

        var errors = DataAnnotationsRouteValidator.Validate<ChedReservationInterventionRouteParams>(null, routeValues);

        errors.Should().NotBeNull();
        errors!.Keys.Should().Contain(nameof(ChedReservationInterventionRouteParams.ChedCertificateId));
        errors.Keys.Should().Contain(nameof(ChedReservationInterventionRouteParams.CustomsDocumentReference));
    }

    [Fact]
    public void ReturnsErrors_When_RouteValuesEmpty()
    {
        var routeValues = new RouteValueDictionary { ["chedId"] = "", ["mrn"] = "" };

        var errors = DataAnnotationsRouteValidator.Validate<ChedReservationInterventionRouteParams>(null, routeValues);

        errors.Should().NotBeNull();
    }

    [Fact]
    public void ReturnsNull_When_RouteValuesPresent()
    {
        var routeValues = new RouteValueDictionary { ["chedId"] = "CHED-123", ["mrn"] = "MRN-123" };

        var errors = DataAnnotationsRouteValidator.Validate<ChedReservationInterventionRouteParams>(null, routeValues);

        errors.Should().BeNull();
    }

    [Fact]
    public void ReturnsNull_When_BoundModelProvided()
    {
        var bound = new ChedReservationInterventionRouteParams { ChedCertificateId = "CHED-123", CustomsDocumentReference = "MRN-123" };

        var errors = DataAnnotationsRouteValidator.Validate<ChedReservationInterventionRouteParams>(bound, new RouteValueDictionary());

        errors.Should().BeNull();
    }
}
