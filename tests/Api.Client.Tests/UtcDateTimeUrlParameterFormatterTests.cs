using Trade.Gateway.Api.Client;

namespace Api.Client.Tests;

public class UtcDateTimeUrlParameterFormatterTests
{
    private readonly UtcDateTimeUrlParameterFormatter _sut = new();

    [Fact]
    public void Format_WhenValueIsUtcDateTime_ReturnsExpectedFormat()
    {
        // Arrange
        var value = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        // Act
        var result = _sut.Format(value, null!, typeof(DateTime));

        // Assert
        Assert.Equal("2024-01-02T03:04:05Z", result);
    }

    [Fact]
    public void Format_WhenValueIsLocalDateTime_ConvertsToUtc()
    {
        // Arrange
        var value = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Local);

        // Act
        var result = _sut.Format(value, null!, typeof(DateTime));

        // Assert
        Assert.Equal(
            value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            result);
    }

    [Fact]
    public void Format_WhenValueIsUnspecifiedDateTime_UsesToUniversalTime()
    {
        // Arrange
        var value = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Unspecified);

        // Act
        var result = _sut.Format(value, null!, typeof(DateTime));

        // Assert
        Assert.Equal(
            value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            result);
    }

    [Fact]
    public void Format_WhenValueIsString_ReturnsString()
    {
        // Arrange
        const string value = "test";

        // Act
        var result = _sut.Format(value, null!, typeof(string));

        // Assert
        Assert.Equal("test", result);
    }

    [Fact]
    public void Format_WhenValueIsInt_ReturnsToString()
    {
        // Arrange
        const int value = 123;

        // Act
        var result = _sut.Format(value, null!, typeof(int));

        // Assert
        Assert.Equal("123", result);
    }

    [Fact]
    public void Format_WhenValueIsNull_ReturnsNull()
    {
        // Act
        var result = _sut.Format(null, null!, typeof(string));

        // Assert
        Assert.Null(result);
    }
}