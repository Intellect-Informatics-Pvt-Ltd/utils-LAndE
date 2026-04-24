using FluentAssertions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="AppLogger{T}"/> verifying delegation to ILogger,
/// scope creation, and checkpoint emission.
/// </summary>
public class AppLoggerTests : IDisposable
{
    private readonly List<LogEvent> _capturedEvents = new();
    private readonly Serilog.Core.Logger _serilogLogger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AppLogger<AppLoggerTests> _appLogger;

    public AppLoggerTests()
    {
        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new InMemoryTestSink(_capturedEvents))
            .CreateLogger();

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(_serilogLogger, dispose: false);
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var msLogger = _loggerFactory.CreateLogger<AppLoggerTests>();
        _appLogger = new AppLogger<AppLoggerTests>(msLogger);
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _serilogLogger.Dispose();
    }

    [Fact]
    public void Debug_DelegatesToILogger_WithDebugLevel()
    {
        _appLogger.Debug("Test debug {Value}", "hello");

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Debug);
    }

    [Fact]
    public void Information_DelegatesToILogger_WithInformationLevel()
    {
        _appLogger.Information("Test info {Value}", "hello");

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void Warning_DelegatesToILogger_WithWarningLevel()
    {
        _appLogger.Warning("Test warning {Value}", "hello");

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void Error_DelegatesToILogger_WithErrorLevel()
    {
        _appLogger.Error("Test error {Value}", "hello");

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public void Critical_DelegatesToILogger_WithFatalLevel()
    {
        _appLogger.Critical("Test critical {Value}", "hello");

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Fatal);
    }

    [Fact]
    public void Error_WithException_DelegatesToILogger()
    {
        var ex = new InvalidOperationException("boom");
        _appLogger.Error(ex, "Something failed");

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Error);
        _capturedEvents[0].Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void BeginScope_PushesPropertiesIntoLogContext()
    {
        var scopeData = new Dictionary<string, object?>
        {
            ["customKey"] = "customValue",
            ["numericKey"] = 42
        };

        using (_appLogger.BeginScope(scopeData))
        {
            _appLogger.Information("Inside scope");
        }

        _capturedEvents.Should().ContainSingle();
        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("customKey");
        evt.Properties["customKey"].ToString().Should().Contain("customValue");
        evt.Properties.Should().ContainKey("numericKey");
    }

    [Fact]
    public void BeginScope_PropertiesRemovedAfterDispose()
    {
        var scopeData = new Dictionary<string, object?> { ["scopedProp"] = "value" };

        using (_appLogger.BeginScope(scopeData))
        {
            _appLogger.Information("Inside scope");
        }

        _appLogger.Information("Outside scope");

        _capturedEvents.Should().HaveCount(2);
        _capturedEvents[0].Properties.Should().ContainKey("scopedProp");
        _capturedEvents[1].Properties.Should().NotContainKey("scopedProp");
    }

    [Fact]
    public void BeginOperation_PushesModuleFeatureOperation()
    {
        using (_appLogger.BeginOperation("Loans", "Disbursement", "Create"))
        {
            _appLogger.Information("Inside operation");
        }

        _capturedEvents.Should().ContainSingle();
        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("module");
        evt.Properties["module"].ToString().Should().Contain("Loans");
        evt.Properties.Should().ContainKey("feature");
        evt.Properties["feature"].ToString().Should().Contain("Disbursement");
        evt.Properties.Should().ContainKey("operation");
        evt.Properties["operation"].ToString().Should().Contain("Create");
    }

    [Fact]
    public void BeginOperation_WithExtraContext_PushesAllProperties()
    {
        var extra = new Dictionary<string, object?> { ["traceId"] = "abc123" };

        using (_appLogger.BeginOperation("FAS", "Voucher", "Post", extra))
        {
            _appLogger.Information("Inside operation with extra");
        }

        _capturedEvents.Should().ContainSingle();
        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("module");
        evt.Properties.Should().ContainKey("traceId");
        evt.Properties["traceId"].ToString().Should().Contain("abc123");
    }

    [Fact]
    public void Checkpoint_EmitsInformationLevelLog()
    {
        _appLogger.Checkpoint("ValidationComplete");

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void Checkpoint_IncludesCheckpointProperty()
    {
        _appLogger.Checkpoint("PaymentInitiated");

        _capturedEvents.Should().ContainSingle();
        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("checkpoint");
        evt.Properties["checkpoint"].ToString().Should().Contain("PaymentInitiated");
    }

    [Fact]
    public void Checkpoint_WithData_IncludesDataProperties()
    {
        var data = new Dictionary<string, object?>
        {
            ["amount"] = 1000,
            ["currency"] = "INR"
        };

        _appLogger.Checkpoint("PaymentProcessed", data);

        _capturedEvents.Should().ContainSingle();
        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("checkpoint");
        evt.Properties.Should().ContainKey("amount");
        evt.Properties.Should().ContainKey("currency");
    }

    [Fact]
    public void Checkpoint_DataPropertiesRemovedAfterEmission()
    {
        var data = new Dictionary<string, object?> { ["tempKey"] = "tempVal" };

        _appLogger.Checkpoint("Step1", data);
        _appLogger.Information("After checkpoint");

        _capturedEvents.Should().HaveCount(2);
        _capturedEvents[0].Properties.Should().ContainKey("tempKey");
        _capturedEvents[1].Properties.Should().NotContainKey("tempKey");
    }

    /// <summary>
    /// In-memory Serilog sink for capturing log events in tests.
    /// </summary>
    internal sealed class InMemoryTestSink : Serilog.Core.ILogEventSink
    {
        private readonly List<LogEvent> _events;

        public InMemoryTestSink(List<LogEvent> events)
        {
            _events = events;
        }

        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }
}
