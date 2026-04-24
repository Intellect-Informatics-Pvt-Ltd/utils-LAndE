using Microsoft.Extensions.Logging;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// Represents a single captured log call made through <see cref="FakeAppLogger{T}"/>.
/// </summary>
/// <param name="Level">The log level of the entry.</param>
/// <param name="Message">The message template passed to the log method.</param>
/// <param name="Exception">The exception associated with the log entry, if any.</param>
/// <param name="Args">The arguments passed to fill the message template placeholders.</param>
/// <param name="Checkpoint">The checkpoint name, if this entry was emitted via <c>Checkpoint</c>.</param>
/// <param name="CheckpointData">The data dictionary passed to <c>Checkpoint</c>, if any.</param>
/// <param name="ScopeData">The scope data active when this entry was emitted.</param>
public sealed record LogEntry(
    LogLevel Level,
    string Message,
    Exception? Exception,
    object[] Args,
    string? Checkpoint,
    IReadOnlyDictionary<string, object?>? CheckpointData,
    IReadOnlyDictionary<string, object?>? ScopeData);
