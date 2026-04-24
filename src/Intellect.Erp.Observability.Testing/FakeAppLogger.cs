using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// In-memory fake implementation of <see cref="IAppLogger{T}"/> that captures all log calls
/// for assertion in unit and integration tests.
/// </summary>
/// <typeparam name="T">The type whose name is used for the logger category.</typeparam>
public sealed class FakeAppLogger<T> : IAppLogger<T>
{
    private readonly ConcurrentBag<LogEntry> _entries = new();
    private readonly AsyncLocal<IReadOnlyDictionary<string, object?>?> _currentScope = new();

    /// <summary>
    /// Gets all captured log entries.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries => _entries.ToArray().Reverse().ToList();

    /// <inheritdoc />
    public void Debug(string messageTemplate, params object[] args)
        => Capture(LogLevel.Debug, null, messageTemplate, args);

    /// <inheritdoc />
    public void Debug(Exception? exception, string messageTemplate, params object[] args)
        => Capture(LogLevel.Debug, exception, messageTemplate, args);

    /// <inheritdoc />
    public void Information(string messageTemplate, params object[] args)
        => Capture(LogLevel.Information, null, messageTemplate, args);

    /// <inheritdoc />
    public void Information(Exception? exception, string messageTemplate, params object[] args)
        => Capture(LogLevel.Information, exception, messageTemplate, args);

    /// <inheritdoc />
    public void Warning(string messageTemplate, params object[] args)
        => Capture(LogLevel.Warning, null, messageTemplate, args);

    /// <inheritdoc />
    public void Warning(Exception? exception, string messageTemplate, params object[] args)
        => Capture(LogLevel.Warning, exception, messageTemplate, args);

    /// <inheritdoc />
    public void Error(string messageTemplate, params object[] args)
        => Capture(LogLevel.Error, null, messageTemplate, args);

    /// <inheritdoc />
    public void Error(Exception? exception, string messageTemplate, params object[] args)
        => Capture(LogLevel.Error, exception, messageTemplate, args);

    /// <inheritdoc />
    public void Critical(string messageTemplate, params object[] args)
        => Capture(LogLevel.Critical, null, messageTemplate, args);

    /// <inheritdoc />
    public void Critical(Exception? exception, string messageTemplate, params object[] args)
        => Capture(LogLevel.Critical, exception, messageTemplate, args);

    /// <inheritdoc />
    public IDisposable BeginScope(IReadOnlyDictionary<string, object?> state)
    {
        var previous = _currentScope.Value;
        var merged = MergeScopes(previous, state);
        _currentScope.Value = merged;
        return new ScopeDisposable(() => _currentScope.Value = previous);
    }

    /// <inheritdoc />
    public IDisposable BeginOperation(
        string module,
        string feature,
        string operation,
        IReadOnlyDictionary<string, object?>? extraContext = null)
    {
        var dict = new Dictionary<string, object?>
        {
            ["module"] = module,
            ["feature"] = feature,
            ["operation"] = operation
        };

        if (extraContext is not null)
        {
            foreach (var kvp in extraContext)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        return BeginScope(dict);
    }

    /// <inheritdoc />
    public void Checkpoint(string checkpoint, IReadOnlyDictionary<string, object?>? data = null)
    {
        var entry = new LogEntry(
            Level: LogLevel.Information,
            Message: $"Checkpoint {checkpoint} reached",
            Exception: null,
            Args: [checkpoint],
            Checkpoint: checkpoint,
            CheckpointData: data,
            ScopeData: _currentScope.Value);

        _entries.Add(entry);
    }

    // ── Assertion helpers ──────────────────────────────────────────

    /// <summary>
    /// Returns true if any log entry was captured at the specified level.
    /// </summary>
    public bool HasLoggedAtLevel(LogLevel level)
        => Entries.Any(e => e.Level == level);

    /// <summary>
    /// Returns true if any log entry's message contains the specified substring.
    /// </summary>
    public bool HasLoggedMessage(string substring)
        => Entries.Any(e => e.Message.Contains(substring, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns true if any log entry was captured at the specified level
    /// with a message containing the specified substring.
    /// </summary>
    public bool HasLoggedMessageAtLevel(LogLevel level, string substring)
        => Entries.Any(e => e.Level == level &&
            e.Message.Contains(substring, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns true if a checkpoint with the specified name was emitted.
    /// </summary>
    public bool HasCheckpoint(string checkpointName)
        => Entries.Any(e => e.Checkpoint == checkpointName);

    /// <summary>
    /// Returns true if any log entry has an associated exception of the specified type.
    /// </summary>
    public bool HasLoggedException<TException>() where TException : Exception
        => Entries.Any(e => e.Exception is TException);

    /// <summary>
    /// Returns true if any log entry has an associated exception.
    /// </summary>
    public bool HasLoggedException()
        => Entries.Any(e => e.Exception is not null);

    /// <summary>
    /// Clears all captured log entries.
    /// </summary>
    public void Clear()
    {
        while (_entries.TryTake(out _)) { }
    }

    // ── Private helpers ────────────────────────────────────────────

    private void Capture(LogLevel level, Exception? exception, string messageTemplate, object[] args)
    {
        var entry = new LogEntry(
            Level: level,
            Message: messageTemplate,
            Exception: exception,
            Args: args,
            Checkpoint: null,
            CheckpointData: null,
            ScopeData: _currentScope.Value);

        _entries.Add(entry);
    }

    private static IReadOnlyDictionary<string, object?> MergeScopes(
        IReadOnlyDictionary<string, object?>? existing,
        IReadOnlyDictionary<string, object?> newScope)
    {
        if (existing is null)
            return new Dictionary<string, object?>(newScope);

        var merged = new Dictionary<string, object?>(existing);
        foreach (var kvp in newScope)
        {
            merged[kvp.Key] = kvp.Value;
        }
        return merged;
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public ScopeDisposable(Action onDispose) => _onDispose = onDispose;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _onDispose();
        }
    }
}
