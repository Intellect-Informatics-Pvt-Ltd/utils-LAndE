using FluentAssertions;
using FluentAssertions.Execution;
using Serilog.Events;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// FluentAssertions extension methods for verifying Serilog <see cref="LogEvent"/> collections
/// captured by <see cref="InMemoryLogSink"/>.
/// </summary>
public static class LogAssertions
{
    /// <summary>
    /// Asserts that the log event collection contains at least one event at the specified level.
    /// </summary>
    /// <param name="events">The log events to search.</param>
    /// <param name="level">The expected log level.</param>
    /// <param name="because">A reason phrase for the assertion message.</param>
    /// <param name="becauseArgs">Arguments for the reason phrase.</param>
    /// <returns>The matching log events for further chaining.</returns>
    public static IReadOnlyList<LogEvent> ShouldContainLogAtLevel(
        this IReadOnlyList<LogEvent> events,
        LogEventLevel level,
        string because = "",
        params object[] becauseArgs)
    {
        var matching = events.Where(e => e.Level == level).ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matching.Count > 0)
            .FailWith("Expected log events to contain at least one entry at level {0}{reason}, but found none.", level);

        return matching;
    }

    /// <summary>
    /// Asserts that the log event collection contains at least one event whose rendered message
    /// contains the specified substring.
    /// </summary>
    /// <param name="events">The log events to search.</param>
    /// <param name="substring">The substring to search for in rendered messages.</param>
    /// <param name="because">A reason phrase for the assertion message.</param>
    /// <param name="becauseArgs">Arguments for the reason phrase.</param>
    /// <returns>The matching log events for further chaining.</returns>
    public static IReadOnlyList<LogEvent> ShouldContainMessage(
        this IReadOnlyList<LogEvent> events,
        string substring,
        string because = "",
        params object[] becauseArgs)
    {
        var matching = events
            .Where(e => e.RenderMessage().Contains(substring, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matching.Count > 0)
            .FailWith("Expected log events to contain a message with \"{0}\"{reason}, but none matched.", substring);

        return matching;
    }

    /// <summary>
    /// Asserts that the log event collection contains at least one event with the specified
    /// property name present.
    /// </summary>
    /// <param name="events">The log events to search.</param>
    /// <param name="propertyName">The property name to look for.</param>
    /// <param name="because">A reason phrase for the assertion message.</param>
    /// <param name="becauseArgs">Arguments for the reason phrase.</param>
    /// <returns>The matching log events for further chaining.</returns>
    public static IReadOnlyList<LogEvent> ShouldContainProperty(
        this IReadOnlyList<LogEvent> events,
        string propertyName,
        string because = "",
        params object[] becauseArgs)
    {
        var matching = events
            .Where(e => e.Properties.ContainsKey(propertyName))
            .ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matching.Count > 0)
            .FailWith("Expected log events to contain property \"{0}\"{reason}, but none had it.", propertyName);

        return matching;
    }

    /// <summary>
    /// Asserts that no log event in the collection contains the specified property name.
    /// </summary>
    /// <param name="events">The log events to search.</param>
    /// <param name="propertyName">The property name that should be absent.</param>
    /// <param name="because">A reason phrase for the assertion message.</param>
    /// <param name="becauseArgs">Arguments for the reason phrase.</param>
    public static void ShouldNotContainProperty(
        this IReadOnlyList<LogEvent> events,
        string propertyName,
        string because = "",
        params object[] becauseArgs)
    {
        var matching = events
            .Where(e => e.Properties.ContainsKey(propertyName))
            .ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matching.Count == 0)
            .FailWith("Expected no log events to contain property \"{0}\"{reason}, but {1} event(s) had it.",
                propertyName, matching.Count);
    }

    // ── InMemoryLogSink convenience overloads ──────────────────────

    /// <summary>
    /// Asserts that the sink contains at least one event at the specified level.
    /// </summary>
    public static IReadOnlyList<LogEvent> ShouldContainLogAtLevel(
        this InMemoryLogSink sink,
        LogEventLevel level,
        string because = "",
        params object[] becauseArgs)
        => sink.Events.ShouldContainLogAtLevel(level, because, becauseArgs);

    /// <summary>
    /// Asserts that the sink contains at least one event whose rendered message
    /// contains the specified substring.
    /// </summary>
    public static IReadOnlyList<LogEvent> ShouldContainMessage(
        this InMemoryLogSink sink,
        string substring,
        string because = "",
        params object[] becauseArgs)
        => sink.Events.ShouldContainMessage(substring, because, becauseArgs);

    /// <summary>
    /// Asserts that the sink contains at least one event with the specified property.
    /// </summary>
    public static IReadOnlyList<LogEvent> ShouldContainProperty(
        this InMemoryLogSink sink,
        string propertyName,
        string because = "",
        params object[] becauseArgs)
        => sink.Events.ShouldContainProperty(propertyName, because, becauseArgs);

    /// <summary>
    /// Asserts that no event in the sink contains the specified property.
    /// </summary>
    public static void ShouldNotContainProperty(
        this InMemoryLogSink sink,
        string propertyName,
        string because = "",
        params object[] becauseArgs)
        => sink.Events.ShouldNotContainProperty(propertyName, because, becauseArgs);
}
