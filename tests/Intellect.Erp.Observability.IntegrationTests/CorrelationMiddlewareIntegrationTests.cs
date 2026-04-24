using System.Text.RegularExpressions;
using FluentAssertions;
using Intellect.Erp.Observability.IntegrationTests.Fixtures;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Integration tests for CorrelationMiddleware using WebApplicationFactory.
/// Validates: Requirements 1.1, 1.2, 1.4
/// </summary>
public class CorrelationMiddlewareIntegrationTests : IClassFixture<ObservabilityWebApplicationFactory>
{
    private readonly HttpClient _client;

    // ULID-26: 26 characters, Crockford Base32 alphabet [0-9A-HJKMNP-TV-Z]
    private static readonly Regex UlidRegex = new(@"^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.Compiled);

    public CorrelationMiddlewareIntegrationTests(ObservabilityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WhenCorrelationIdHeaderSent_SameValueEchoedInResponse()
    {
        // Arrange
        var correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");
        request.Headers.Add("X-Correlation-Id", correlationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.First().Should().Be(correlationId);
    }

    [Fact]
    public async Task WhenNoCorrelationHeader_GeneratedUlid26EchoedInResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        var generatedId = values!.First();
        generatedId.Should().MatchRegex(UlidRegex.ToString());
        generatedId.Should().HaveLength(26);
    }

    [Fact]
    public async Task ResponseAlwaysHasCorrelationIdHeader()
    {
        // Arrange & Act — request without correlation header
        var response1 = await _client.GetAsync("/test/ok");
        response1.Headers.Contains("X-Correlation-Id").Should().BeTrue();

        // Arrange & Act — request with correlation header
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");
        request.Headers.Add("X-Correlation-Id", "test-id-123");
        var response2 = await _client.SendAsync(request);
        response2.Headers.Contains("X-Correlation-Id").Should().BeTrue();

        // Arrange & Act — error response
        var response3 = await _client.GetAsync("/test/throw/not-found");
        response3.Headers.Contains("X-Correlation-Id").Should().BeTrue();
    }
}
