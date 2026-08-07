using System.Net;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Options;
using NSubstitute;
using Trade.Gateway.Api.Client;
using Trade.Gateway.Api.Client.DelegatingHandlers;

namespace Api.Client.Tests;

public class StsAuthDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_Adds_Bearer_Token_To_Request()
    {
        // Arrange
        var sts = Substitute.For<IAmazonSecurityTokenService>();

        sts.GetWebIdentityTokenAsync(
                Arg.Any<GetWebIdentityTokenRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetWebIdentityTokenResponse
            {
                WebIdentityToken = "token-123",
                Expiration = DateTime.UtcNow.AddMinutes(10)
            });

        HttpRequestMessage? capturedRequest = null;

        var handler = new StsAuthDelegatingHandler(
            sts,
            Options.Create(new TracesGatewayOptions
            {
                BaseUrl = "local",
                Audience = "audience",
                DurationSeconds = 900
            }))
        {
            InnerHandler = new TestHttpMessageHandler(request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("https://example.com");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("token-123", capturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_Caches_Token()
    {
        // Arrange
        var sts = Substitute.For<IAmazonSecurityTokenService>();

        sts.GetWebIdentityTokenAsync(
                Arg.Any<GetWebIdentityTokenRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetWebIdentityTokenResponse
            {
                WebIdentityToken = "cached-token",
                Expiration = DateTime.UtcNow.AddMinutes(10)
            });

        var handler = new StsAuthDelegatingHandler(
            sts,
            Options.Create(new TracesGatewayOptions
            {
                BaseUrl = "local",
                Audience = "audience",
                DurationSeconds = 900
            }))
        {
            InnerHandler = new TestHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK))
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("https://example.com");
        await client.GetAsync("https://example.com");

        // Assert
        await sts.Received(1).GetWebIdentityTokenAsync(
            Arg.Any<GetWebIdentityTokenRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_Refreshes_Expired_Token()
    {
        // Arrange
        var sts = Substitute.For<IAmazonSecurityTokenService>();

        sts.GetWebIdentityTokenAsync(
                Arg.Any<GetWebIdentityTokenRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(
                new GetWebIdentityTokenResponse
                {
                    WebIdentityToken = "token-1",
                    Expiration = DateTime.UtcNow.AddSeconds(5)
                },
                new GetWebIdentityTokenResponse
                {
                    WebIdentityToken = "token-2",
                    Expiration = DateTime.UtcNow.AddMinutes(10)
                });

        var handler = new StsAuthDelegatingHandler(
            sts,
            Options.Create(new TracesGatewayOptions
            {
                BaseUrl = "local",
                Audience = "audience",
                DurationSeconds = 900
            }))
        {
            InnerHandler = new TestHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK))
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("https://example.com");
        await client.GetAsync("https://example.com");

        // Assert
        await sts.Received(2).GetWebIdentityTokenAsync(
            Arg.Any<GetWebIdentityTokenRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_Passes_Correct_Request_To_Sts()
    {
        // Arrange
        var sts = Substitute.For<IAmazonSecurityTokenService>();

        sts.GetWebIdentityTokenAsync(
                Arg.Any<GetWebIdentityTokenRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetWebIdentityTokenResponse
            {
                WebIdentityToken = "token",
                Expiration = DateTime.UtcNow.AddMinutes(10)
            });

        var handler = new StsAuthDelegatingHandler(
            sts,
            Options.Create(new TracesGatewayOptions
            {
                BaseUrl = "local",
                Audience = "expected-audience",
                DurationSeconds = 1234
            }))
        {
            InnerHandler = new TestHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK))
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("https://example.com");

        // Assert
        await sts.Received(1).GetWebIdentityTokenAsync(
            Arg.Is<GetWebIdentityTokenRequest>(r =>
                r!.Audience.Single() == "expected-audience" &&
                r.DurationSeconds == 1234 &&
                r.SigningAlgorithm == "RS256"),
            Arg.Any<CancellationToken>());
    }

    
}