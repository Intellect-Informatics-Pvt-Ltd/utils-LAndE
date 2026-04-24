using FluentAssertions;
using Intellect.Erp.Observability.IntegrationTests.Fixtures;
using Serilog.Events;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Integration tests for RequestLoggingMiddleware.
/// Validates: Requirements 7.4, 7.5
/// </summary>
public class RequestLoggingMiddlewareIntegrationTests : IClassFixture<ObservabilityWebApplicationFactory>, IDisposable
{
    private readonly ObservabilityWebApplicationFactory _factory;
    private readonly InMemoryTestSink _logSink;

    public RequestLoggingMiddlewareIntegrationTests(ObservabilityWebApplicationFactory factory)
    {
        _factory = factory;
        _logSink = factory.LogSink;
    }

    [Fact]
    public async Task RequestDuration_IsLogged()
    {
        // Arrange
        var client = _factory.CreateClient();
        _logSink.Clear();

        // Act
        await client.GetAsync("/test/ok");

        // Assert — look for log entries containing DurationMs in the message template
        var logEvents = _logSink.Events;
        var hasDuration = logEvents.Any(e =>
            e.MessageTemplate.Text.Contains("DurationMs"));

        hasDuration.Should().BeTrue("request logging should include duration in the message template");
    }

    [Fact]
    public async Task SlowRequest_TriggersWarningLevel()
    {
        // Arrange — SlowRequestThresholdMs is set to 50ms in test config
        var client = _factory.CreateClient();
        _logSink.Clear();

        // Act — /test/slow endpoint delays 100ms, exceeding the 50ms threshold
        await client.GetAsync("/test/slow");

        // Assert — at least one Warning-level log entry should exist for the slow request
        var logEvents = _logSink.Events;
        var hasSlowWarning = logEvents.Any(e =>
            e.Level == LogEventLevel.Warning &&
            e.MessageTemplate.Text.Contains("SLOW"));

        hasSlowWarning.Should().BeTrue("slow requests should trigger Warning level log with SLOW marker");
    }

    [Fact]
    public async Task ExcludedPaths_AreNotLogged()
    {
        // Arrange — /health and /metrics are excluded in test config
        var client = _factory.CreateClient();
        _logSink.Clear();

        // Act
        await client.GetAsync("/health");
        await client.GetAsync("/metrics");

        // Assert — no request logging entries for excluded paths
        var logEvents = _logSink.Events;
        var hasHealthLog = logEvents.Any(e =>
            e.MessageTemplate.Text.Contains("/health") &&
            (e.MessageTemplate.Text.Contains("responded") || e.MessageTemplate.Text.Contains("started")));
        var hasMetricsLog = logEvents.Any(e =>
            e.MessageTemplate.Text.Contains("/metrics") &&
            (e.MessageTemplate.Text.Contains("responded") || e.MessageTemplate.Text.Contains("started")));

        hasHealthLog.Should().BeFalse("excluded path /health should not be logged");
        hasMetricsLog.Should().BeFalse("excluded path /metrics should not be logged");
    }

    public void Dispose()
    {
        _logSink.Clear();
    }
}
