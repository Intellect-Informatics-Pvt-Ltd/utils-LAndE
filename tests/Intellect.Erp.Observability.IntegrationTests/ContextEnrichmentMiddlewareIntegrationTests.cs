using FluentAssertions;
using Intellect.Erp.Observability.IntegrationTests.Fixtures;
using Serilog.Events;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Integration tests for ContextEnrichmentMiddleware.
/// Validates: Requirements 2.6
///
/// Note: The ContextEnrichmentMiddleware reads user context from HttpContext.User claims
/// and tenant context from request headers. Since UseObservability() registers the middleware
/// pipeline before UseAuthentication(), we test tenant context via headers (which are always
/// available) and verify that unauthenticated requests don't have user context fields.
/// </summary>
public class ContextEnrichmentMiddlewareIntegrationTests : IClassFixture<ObservabilityWebApplicationFactory>, IDisposable
{
    private readonly ObservabilityWebApplicationFactory _factory;
    private readonly InMemoryTestSink _logSink;

    public ContextEnrichmentMiddlewareIntegrationTests(ObservabilityWebApplicationFactory factory)
    {
        _factory = factory;
        _logSink = factory.LogSink;
    }

    [Fact]
    public async Task RequestWithTenantHeaders_HasTenantContextFieldsInLogs()
    {
        // Arrange — send tenant context via headers (doesn't require authentication)
        var client = _factory.CreateClient();
        _logSink.Clear();

        var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");
        request.Headers.Add("X-Tenant-Id", "tenant-456");
        request.Headers.Add("X-State-Code", "KA");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var logEvents = _logSink.Events;

        // The ContextEnrichmentMiddleware pushes tenant context from headers into LogContext.
        // Check that at least one log event has TenantId or StateCode properties.
        var hasTenantProperty = logEvents.Any(e =>
            e.Properties.ContainsKey("TenantId"));
        var hasStateCodeProperty = logEvents.Any(e =>
            e.Properties.ContainsKey("StateCode"));

        hasTenantProperty.Should().BeTrue("request with X-Tenant-Id header should have TenantId in log context");
        hasStateCodeProperty.Should().BeTrue("request with X-State-Code header should have StateCode in log context");
    }

    [Fact]
    public async Task UnauthenticatedRequest_DoesNotHaveUserContextFields()
    {
        // Arrange
        var client = _factory.CreateClient();
        _logSink.Clear();

        // Act
        var response = await client.GetAsync("/test/ok");

        // Assert
        response.EnsureSuccessStatusCode();

        var logEvents = _logSink.Events;
        var hasUserIdProperty = logEvents.Any(e =>
            e.Properties.ContainsKey("UserId"));

        hasUserIdProperty.Should().BeFalse("unauthenticated request should not have UserId in log context");
    }

    public void Dispose()
    {
        _logSink.Clear();
    }
}
