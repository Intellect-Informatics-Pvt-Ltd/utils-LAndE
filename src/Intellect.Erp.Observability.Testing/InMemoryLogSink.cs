using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// Thread-safe Serilog sink that captures <see cref="LogEvent"/> instances in memory
/// for assertion in unit and integration tests.
/// </summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    /// <summary>
    /// Gets all captured log events in the order they were emitted.
    /// </summary>
    public IReadOnlyList<LogEvent> Events => _events.ToArray();

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        _events.Enqueue(logEvent);
    }

    /// <summary>
    /// Removes all captured log events. Useful for test cleanup between test cases.
    /// </summary>
    public void Clear()
    {
        while (_events.TryDequeue(out _)) { }
    }
}
