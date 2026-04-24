using FluentAssertions;
using Intellect.Erp.Observability.Testing;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Testing;

/// <summary>
/// Unit tests for <see cref="LogAssertions"/> FluentAssertions extensions.
/// </summary>
public class LogAssertionsTests
{
    private readonly InMemoryLogSink _sink = new();

    private Logger CreateLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("correlationId", "test-corr")
            .Enrich.WithProperty("module", "TestModule")
            .WriteTo.Sink(_sink)
            .CreateLogger();

    [Fact]
    public void ShouldContainLogAtLevel_Passes_WhenLevelExists()
    {
        using var logger = CreateLogger();
        logger.Warning("A warning");

        var result = _sink.Events.ShouldContainLogAtLevel(LogEventLevel.Warning);
        result.Should().HaveCount(1);
    }

    [Fact]
    public void ShouldContainLogAtLevel_Fails_WhenLevelMissing()
    {
        using var logger = CreateLogger();
        logger.Information("Info only");

        var act = () => _sink.Events.ShouldContainLogAtLevel(LogEventLevel.Error);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ShouldContainMessage_Passes_WhenSubstringFound()
    {
        using var logger = CreateLogger();
        logger.Information("Order {OrderId} processed", "ORD-123");

        var result = _sink.Events.ShouldContainMessage("ORD-123");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void ShouldContainMessage_Fails_WhenSubstringMissing()
    {
        using var logger = CreateLogger();
        logger.Information("Hello world");

        var act = () => _sink.Events.ShouldContainMessage("nonexistent");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ShouldContainProperty_Passes_WhenPropertyExists()
    {
        using var logger = CreateLogger();
        logger.Information("Test");

        var result = _sink.Events.ShouldContainProperty("correlationId");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void ShouldContainProperty_Fails_WhenPropertyMissing()
    {
        using var logger = CreateLogger();
        logger.Information("Test");

        var act = () => _sink.Events.ShouldContainProperty("nonExistentProp");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ShouldNotContainProperty_Passes_WhenPropertyAbsent()
    {
        using var logger = CreateLogger();
        logger.Information("Test");

        _sink.Events.ShouldNotContainProperty("secretField");
    }

    [Fact]
    public void ShouldNotContainProperty_Fails_WhenPropertyPresent()
    {
        using var logger = CreateLogger();
        logger.Information("Test");

        var act = () => _sink.Events.ShouldNotContainProperty("correlationId");
        act.Should().Throw<Exception>();
    }

    // ── InMemoryLogSink convenience overloads ──────────────────────

    [Fact]
    public void SinkOverload_ShouldContainLogAtLevel_Works()
    {
        using var logger = CreateLogger();
        logger.Error("An error");

        var result = _sink.ShouldContainLogAtLevel(LogEventLevel.Error);
        result.Should().HaveCount(1);
    }

    [Fact]
    public void SinkOverload_ShouldContainMessage_Works()
    {
        using var logger = CreateLogger();
        logger.Information("Payment completed");

        var result = _sink.ShouldContainMessage("Payment");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void SinkOverload_ShouldContainProperty_Works()
    {
        using var logger = CreateLogger();
        logger.Information("Test");

        var result = _sink.ShouldContainProperty("module");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void SinkOverload_ShouldNotContainProperty_Works()
    {
        using var logger = CreateLogger();
        logger.Information("Test");

        _sink.ShouldNotContainProperty("absentProp");
    }
}
