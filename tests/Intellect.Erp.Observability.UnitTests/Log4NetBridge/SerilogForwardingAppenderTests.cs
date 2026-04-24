using FluentAssertions;
using Intellect.Erp.Observability.Log4NetBridge;
using log4net.Core;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Log4NetBridge;

/// <summary>
/// Unit tests for <see cref="SerilogForwardingAppender"/> verifying level mapping,
/// message/exception preservation, and backpressure behavior.
/// </summary>
public sealed class SerilogForwardingAppenderTests : IDisposable
{
    private readonly List<LogEvent> _capturedEvents = new();
    private readonly Serilog.Core.Logger _serilogLogger;

    public SerilogForwardingAppenderTests()
    {
        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new InMemoryTestSink(_capturedEvents))
            .CreateLogger();

        // Set the global Serilog logger so Log.Write works
        Log.Logger = _serilogLogger;

        // Reset the static dropped counter before each test
        SerilogForwardingAppender.ResetDroppedCount();
    }

    public void Dispose()
    {
        Log.CloseAndFlush();
        _serilogLogger.Dispose();
    }

    #region Level Mapping Tests

    [Fact]
    public void MapLevel_DebugLevel_ReturnsSerilogDebug()
    {
        SerilogForwardingAppender.MapLevel(Level.Debug)
            .Should().Be(LogEventLevel.Debug);
    }

    [Fact]
    public void MapLevel_InfoLevel_ReturnsSerilogInformation()
    {
        SerilogForwardingAppender.MapLevel(Level.Info)
            .Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void MapLevel_WarnLevel_ReturnsSerilogWarning()
    {
        SerilogForwardingAppender.MapLevel(Level.Warn)
            .Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void MapLevel_ErrorLevel_ReturnsSerilogError()
    {
        SerilogForwardingAppender.MapLevel(Level.Error)
            .Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public void MapLevel_FatalLevel_ReturnsSerilogFatal()
    {
        SerilogForwardingAppender.MapLevel(Level.Fatal)
            .Should().Be(LogEventLevel.Fatal);
    }

    [Fact]
    public void MapLevel_NullLevel_ReturnsSerilogInformation()
    {
        SerilogForwardingAppender.MapLevel(null)
            .Should().Be(LogEventLevel.Information);
    }

    #endregion

    #region Message Preservation Tests

    [Fact]
    public void Append_PreservesMessage()
    {
        var appender = CreateAppender();
        var loggingEvent = CreateLoggingEvent(Level.Info, "Test message from log4net");

        appender.DoAppend(loggingEvent);
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].MessageTemplate.Text.Should().Be("Test message from log4net");
    }

    [Fact]
    public void Append_PreservesMessageWithSpecialCharacters()
    {
        var appender = CreateAppender();
        var loggingEvent = CreateLoggingEvent(Level.Info, "Message with {braces} and \"quotes\"");

        appender.DoAppend(loggingEvent);
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].MessageTemplate.Text.Should().Contain("braces");
    }

    #endregion

    #region Exception Preservation Tests

    [Fact]
    public void Append_PreservesException()
    {
        var appender = CreateAppender();
        var exception = new InvalidOperationException("Something went wrong");
        var loggingEvent = CreateLoggingEvent(Level.Error, "Error occurred", exception);

        appender.DoAppend(loggingEvent);
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Append_WithNoException_SerilogEventHasNullException()
    {
        var appender = CreateAppender();
        var loggingEvent = CreateLoggingEvent(Level.Info, "No exception here");

        appender.DoAppend(loggingEvent);
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Exception.Should().BeNull();
    }

    #endregion

    #region Backpressure Tests

    [Fact]
    public void Append_WhenQueueExceedsCapacity_DropsOldestEntries()
    {
        const int capacity = 5;
        var appender = CreateAppender(maxQueueSize: capacity);

        // Enqueue more than capacity
        for (var i = 0; i < capacity + 3; i++)
        {
            appender.DoAppend(CreateLoggingEvent(Level.Info, $"Message {i}"));
        }

        // Drain and check: we should have at most `capacity` events
        appender.DrainQueue();

        _capturedEvents.Count.Should().BeLessOrEqualTo(capacity);
    }

    [Fact]
    public void Append_WhenQueueExceedsCapacity_IncrementsDroppedCounter()
    {
        const int capacity = 5;
        var appender = CreateAppender(maxQueueSize: capacity);

        SerilogForwardingAppender.DroppedCount.Should().Be(0);

        // Enqueue more than capacity
        for (var i = 0; i < capacity + 3; i++)
        {
            appender.DoAppend(CreateLoggingEvent(Level.Info, $"Message {i}"));
        }

        SerilogForwardingAppender.DroppedCount.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void Append_WhenWithinCapacity_DoesNotDrop()
    {
        const int capacity = 10;
        var appender = CreateAppender(maxQueueSize: capacity);

        for (var i = 0; i < capacity; i++)
        {
            appender.DoAppend(CreateLoggingEvent(Level.Info, $"Message {i}"));
        }

        SerilogForwardingAppender.DroppedCount.Should().Be(0);

        appender.DrainQueue();
        _capturedEvents.Should().HaveCount(capacity);
    }

    [Fact]
    public void Append_DroppedCounterIsStatic_AccumulatesAcrossInstances()
    {
        var appender1 = CreateAppender(maxQueueSize: 2);
        var appender2 = CreateAppender(maxQueueSize: 2);

        // Overflow appender1
        for (var i = 0; i < 5; i++)
            appender1.DoAppend(CreateLoggingEvent(Level.Info, $"A{i}"));

        var countAfterFirst = SerilogForwardingAppender.DroppedCount;
        countAfterFirst.Should().BeGreaterOrEqualTo(3);

        // Overflow appender2
        for (var i = 0; i < 5; i++)
            appender2.DoAppend(CreateLoggingEvent(Level.Info, $"B{i}"));

        SerilogForwardingAppender.DroppedCount.Should().BeGreaterThan(countAfterFirst);
    }

    #endregion

    #region Level Mapping Through Full Pipeline

    [Fact]
    public void Append_DebugEvent_ForwardedAsSerilogDebug()
    {
        var appender = CreateAppender();
        appender.DoAppend(CreateLoggingEvent(Level.Debug, "debug msg"));
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle()
            .Which.Level.Should().Be(LogEventLevel.Debug);
    }

    [Fact]
    public void Append_InfoEvent_ForwardedAsSerilogInformation()
    {
        var appender = CreateAppender();
        appender.DoAppend(CreateLoggingEvent(Level.Info, "info msg"));
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle()
            .Which.Level.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void Append_WarnEvent_ForwardedAsSerilogWarning()
    {
        var appender = CreateAppender();
        appender.DoAppend(CreateLoggingEvent(Level.Warn, "warn msg"));
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle()
            .Which.Level.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void Append_ErrorEvent_ForwardedAsSerilogError()
    {
        var appender = CreateAppender();
        appender.DoAppend(CreateLoggingEvent(Level.Error, "error msg"));
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle()
            .Which.Level.Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public void Append_FatalEvent_ForwardedAsSerilogFatal()
    {
        var appender = CreateAppender();
        appender.DoAppend(CreateLoggingEvent(Level.Fatal, "fatal msg"));
        appender.DrainQueue();

        _capturedEvents.Should().ContainSingle()
            .Which.Level.Should().Be(LogEventLevel.Fatal);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void MaxQueueSize_DefaultsToDefaultMaxQueueSize()
    {
        var appender = new SerilogForwardingAppender();
        appender.MaxQueueSize.Should().Be(SerilogForwardingAppender.DefaultMaxQueueSize);
    }

    [Fact]
    public void MaxQueueSize_CanBeConfigured()
    {
        var appender = new SerilogForwardingAppender { MaxQueueSize = 500 };
        appender.MaxQueueSize.Should().Be(500);
    }

    [Fact]
    public void MaxQueueSize_InvalidValue_ResetsToDefault()
    {
        var appender = new SerilogForwardingAppender { MaxQueueSize = -1 };
        appender.MaxQueueSize.Should().Be(SerilogForwardingAppender.DefaultMaxQueueSize);
    }

    #endregion

    #region Helpers

    private static SerilogForwardingAppender CreateAppender(int maxQueueSize = SerilogForwardingAppender.DefaultMaxQueueSize)
    {
        var appender = new SerilogForwardingAppender
        {
            MaxQueueSize = maxQueueSize
        };
        // Note: We don't call ActivateOptions() to avoid starting the background thread.
        // Instead we call DrainQueue() manually in tests.
        return appender;
    }

    private static LoggingEvent CreateLoggingEvent(Level level, string message, Exception? exception = null)
    {
        if (exception != null)
        {
            return new LoggingEvent(
                typeof(SerilogForwardingAppenderTests),
                null,
                "TestLogger",
                level,
                message,
                exception);
        }

        var loggingEventData = new LoggingEventData
        {
            Level = level,
            Message = message,
            LoggerName = "TestLogger",
            TimeStampUtc = DateTime.UtcNow
        };

        return new LoggingEvent(loggingEventData);
    }

    private sealed class InMemoryTestSink : Serilog.Core.ILogEventSink
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

    #endregion
}
