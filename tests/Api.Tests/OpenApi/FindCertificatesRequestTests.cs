using System.ComponentModel.DataAnnotations;
using Api.Models;
using AwesomeAssertions;

namespace Api.Tests.OpenApi;

public class FindCertificatesRequestTests
{
    [Fact]
    public void Should_Have_Expected_Default_Values()
    {
        // Arrange
        var request = new FindCertificatesRequest();

        // Assert
        request.PageSize.Should().Be(10);
        request.Offset.Should().Be(0);
        request.AcceptLanguage.Should().Be("en");
        request.UpdatedFrom.Should().BeNull();
        request.UpdatedBefore.Should().BeNull();
    }

    [Fact]
    public void Should_Fail_When_UpdatedFrom_Is_Null()
    {
        // Arrange
        var request = new FindCertificatesRequest { UpdatedBefore = DateTime.UtcNow };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(x => x.MemberNames.Contains(nameof(FindCertificatesRequest.UpdatedFrom)));
    }

    [Fact]
    public void Should_Fail_When_UpdatedBefore_Is_Null()
    {
        // Arrange
        var request = new FindCertificatesRequest { UpdatedFrom = DateTime.UtcNow };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().ContainSingle(x => x.MemberNames.Contains(nameof(FindCertificatesRequest.UpdatedBefore)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Fail_When_Offset_Is_Less_Than_One(int offset)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Offset = offset;

        // Act
        var results = ValidateModel(request);

        // Assert
        results
            .Should()
            .Contain(x =>
                x.MemberNames.Contains(nameof(FindCertificatesRequest.Offset))
                && x.ErrorMessage == "offset must be equal to or greater than 0"
            );
    }

    [Fact]
    public void Should_Fail_When_UpdatedFrom_Is_Not_Utc()
    {
        // Arrange
        var request = CreateValidRequest();
        request.UpdatedFrom = new DateTimeOffset(
            new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Unspecified),
            TimeSpan.FromHours(1)
        );

        // Act
        var results = ValidateModel(request);

        // Assert
        results
            .Should()
            .Contain(x =>
                x.MemberNames.Contains(nameof(FindCertificatesRequest.UpdatedFrom))
                && x.ErrorMessage == "UpdatedFrom date must be UTC."
            );
    }

    [Fact]
    public void Should_Fail_When_UpdatedBefore_Is_Not_Utc()
    {
        // Arrange
        var request = CreateValidRequest();
        request.UpdatedBefore = new DateTimeOffset(
            new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Unspecified),
            TimeSpan.FromHours(1)
        );

        // Act
        var results = ValidateModel(request);

        // Assert
        results
            .Should()
            .Contain(x =>
                x.MemberNames.Contains(nameof(FindCertificatesRequest.UpdatedBefore))
                && x.ErrorMessage == "UpdatedBefore date must be UTC."
            );
    }

    [Fact]
    public void Should_Fail_When_UpdatedBefore_Is_Earlier_Than_UpdatedFrom()
    {
        // Arrange
        var request = CreateValidRequest();
        request.UpdatedFrom = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        request.UpdatedBefore = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var results = ValidateModel(request);

        // Assert
        results
            .Should()
            .Contain(x =>
                x.MemberNames.Contains(nameof(FindCertificatesRequest.UpdatedFrom))
                && x.ErrorMessage == "UpdatedBefore must be greater than or equal to UpdatedFrom."
            );
    }

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateModel(FindCertificatesRequest model)
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

    private static FindCertificatesRequest CreateValidRequest()
    {
        return new FindCertificatesRequest
        {
            PageSize = 10,
            Offset = 1,
            AcceptLanguage = "en",
            UpdatedFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedBefore = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };
    }
}
