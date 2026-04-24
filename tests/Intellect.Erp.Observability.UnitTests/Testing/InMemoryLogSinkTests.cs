using FluentAssertions;
using Intellect.Erp.Observability.Testing;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Testing;

/// <summary>
/// Unit tests for <see cref="InMemoryLogSink"/>.
/// </summary>
public class InMemoryLogSinkTests
{
    [Fact]
    public void Emit_CapturesLogEvent()
    {
        var sink = new InMemoryLogSink();

        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Test message");

        sink.Events.Should().HaveCount(1);
        sink.Events[0].Level.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void Emit_CapturesMultipleEvents()
    {
        var sink = new InMemoryLogSink();

        using var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Debug("Debug");
        logger.Information("Info");
        logger.Warning("Warn");

        sink.Events.Should().HaveCount(3);
    }

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        var sink = new InMemoryLogSink();

        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Message 1");
        logger.Information("Message 2");

        sink.Clear();

        sink.Events.Should().BeEmpty();
    }

    [Fact]
    public void Events_PreservesOrder()
    {
        var sink = new InMemoryLogSink();

        using var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Debug("First");
        logger.Information("Second");
        logger.Warning("Third");

        sink.Events[0].RenderMessage().Should().Contain("First");
        sink.Events[1].RenderMessage().Should().Contain("Second");
        sink.Events[2].RenderMessage().Should().Contain("Third");
    }

    [Fact]
    public void Emit_CapturesProperties()
    {
        var sink = new InMemoryLogSink();

        using var logger = new LoggerConfiguration()
            .Enrich.WithProperty("testProp", "testValue")
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("With property");

        sink.Events[0].Properties.Should().ContainKey("testProp");
    }

    [Fact]
    public void IsThreadSafe()
    {
        var sink = new InMemoryLogSink();

        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        Parallel.For(0, 100, i =>
        {
            logger.Information("Message {Index}", i);
        });

        sink.Events.Should().HaveCount(100);
    }
}
