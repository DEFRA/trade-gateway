using System.ComponentModel.DataAnnotations;
using Api.Models;
using AwesomeAssertions;

namespace Api.Tests.Validation;

public class ChedReservationInterventionRouteParamsTests
{
    [Fact]
    public void Should_Fail_When_ChedCertificateId_Is_Null_Or_Whitespace()
    {
        var model = new ChedReservationInterventionRouteParams { ChedCertificateId = "", CustomsDocumentReference = "MRN-123" };

        var results = ValidateModel(model);

        results.Should().ContainSingle(x => x.MemberNames.Contains(nameof(ChedReservationInterventionRouteParams.ChedCertificateId)));
    }

    [Fact]
    public void Should_Fail_When_CustomsDocumentReference_Is_Null_Or_Whitespace()
    {
        var model = new ChedReservationInterventionRouteParams { ChedCertificateId = "CHED-123", CustomsDocumentReference = "" };

        var results = ValidateModel(model);

        results.Should().ContainSingle(x => x.MemberNames.Contains(nameof(ChedReservationInterventionRouteParams.CustomsDocumentReference)));
    }

    [Fact]
    public void Should_Pass_When_Both_Route_Parameters_Are_Present()
    {
        var model = new ChedReservationInterventionRouteParams { ChedCertificateId = "CHED-123", CustomsDocumentReference = "MRN-123" };

        var results = ValidateModel(model);

        results.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateModel(ChedReservationInterventionRouteParams model)
    {
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true
        );

        return validationResults;
    }
}
