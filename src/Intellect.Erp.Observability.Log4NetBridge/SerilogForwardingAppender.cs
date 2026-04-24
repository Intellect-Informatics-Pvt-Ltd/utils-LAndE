using System.Collections.Concurrent;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using Serilog;
using Serilog.Events;

namespace Intellect.Erp.Observability.Log4NetBridge;

/// <summary>
/// A log4net appender that forwards log events into the Serilog pipeline.
/// Uses a lock-free <see cref="ConcurrentQueue{T}"/> with bounded capacity
/// and a background flush thread to drain events into Serilog.
/// </summary>
public sealed class SerilogForwardingAppender : AppenderSkeleton
{
    /// <summary>
    /// Default maximum number of events the internal queue can hold before
    /// oldest entries are dropped.
    /// </summary>
    public const int DefaultMaxQueueSize = 10_000;

    private readonly ConcurrentQueue<LoggingEvent> _queue = new();
    private volatile int _queueCount;
    private int _maxQueueSize = DefaultMaxQueueSize;
    private Thread? _flushThread;
    private volatile bool _running;

    /// <summary>
    /// Counter for events dropped due to backpressure. Incremented atomically.
    /// </summary>
    private static long _droppedCount;

    /// <summary>
    /// Gets the total number of events dropped across all instances due to backpressure.
    /// Exposed as <c>observability.log4net.dropped</c> telemetry counter.
    /// </summary>
    public static long DroppedCount => Interlocked.Read(ref _droppedCount);

    /// <summary>
    /// Gets or sets the maximum queue size. When the queue exceeds this capacity,
    /// oldest entries are dropped. Defaults to <see cref="DefaultMaxQueueSize"/>.
    /// </summary>
    public int MaxQueueSize
    {
        get => _maxQueueSize;
        set => _maxQueueSize = value > 0 ? value : DefaultMaxQueueSize;
    }

    /// <summary>
    /// Gets or sets the interval in milliseconds between flush cycles.
    /// Defaults to 100ms.
    /// </summary>
    public int FlushIntervalMs { get; set; } = 100;

    /// <inheritdoc />
    public override void ActivateOptions()
    {
        base.ActivateOptions();
        _running = true;
        _flushThread = new Thread(FlushLoop)
        {
            Name = "SerilogForwardingAppender-Flush",
            IsBackground = true
        };
        _flushThread.Start();
    }

    /// <inheritdoc />
    protected override void Append(LoggingEvent loggingEvent)
    {
        _queue.Enqueue(loggingEvent);
        Interlocked.Increment(ref _queueCount);

        // Enforce bounded capacity: drop oldest entries when over limit
        while (Interlocked.CompareExchange(ref _queueCount, 0, 0) > _maxQueueSize)
        {
            if (_queue.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _queueCount);
                Interlocked.Increment(ref _droppedCount);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Background loop that drains the queue into the Serilog pipeline.
    /// </summary>
    private void FlushLoop()
    {
        while (_running)
        {
            DrainQueue();
            Thread.Sleep(FlushIntervalMs);
        }

        // Final drain on shutdown
        DrainQueue();
    }

    /// <summary>
    /// Drains all currently queued events into the Serilog pipeline.
    /// </summary>
    internal void DrainQueue()
    {
        while (_queue.TryDequeue(out var evt))
        {
            Interlocked.Decrement(ref _queueCount);
            ForwardToSerilog(evt);
        }
    }

    /// <summary>
    /// Maps a log4net <see cref="LoggingEvent"/> to a Serilog log call.
    /// </summary>
    private static void ForwardToSerilog(LoggingEvent loggingEvent)
    {
        var level = MapLevel(loggingEvent.Level);
        var message = loggingEvent.RenderedMessage;
        var exception = loggingEvent.ExceptionObject;

        Log.Write(level, exception, message ?? string.Empty);
    }

    /// <summary>
    /// Maps a log4net <see cref="Level"/> to a Serilog <see cref="LogEventLevel"/>.
    /// </summary>
    internal static LogEventLevel MapLevel(Level? log4netLevel)
    {
        if (log4netLevel == null)
            return LogEventLevel.Information;

        // log4net levels are compared by their numeric Value property.
        // Level.Debug.Value = 30000, Level.Info.Value = 40000,
        // Level.Warn.Value = 60000, Level.Error.Value = 70000,
        // Level.Fatal.Value = 110000
        if (log4netLevel.Value < Level.Info.Value)
            return LogEventLevel.Debug;

        if (log4netLevel.Value < Level.Warn.Value)
            return LogEventLevel.Information;

        if (log4netLevel.Value < Level.Error.Value)
            return LogEventLevel.Warning;

        if (log4netLevel.Value < Level.Fatal.Value)
            return LogEventLevel.Error;

        return LogEventLevel.Fatal;
    }

    /// <summary>
    /// Resets the static dropped counter. Intended for testing only.
    /// </summary>
    internal static void ResetDroppedCount()
    {
        Interlocked.Exchange(ref _droppedCount, 0);
    }

    /// <inheritdoc />
    protected override void OnClose()
    {
        _running = false;
        _flushThread?.Join(TimeSpan.FromSeconds(5));
        base.OnClose();
    }
}
