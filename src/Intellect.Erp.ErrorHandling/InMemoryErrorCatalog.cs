using System.Collections.Immutable;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IErrorCatalog"/>
/// backed by an immutable dictionary.
/// </summary>
public sealed class InMemoryErrorCatalog : IErrorCatalog
{
    /// <summary>
    /// The default fallback error code used when a requested code is not found.
    /// </summary>
    public const string DefaultFallbackCode = "ERP-CORE-SYS-0001";

    private static readonly ErrorCatalogEntry FallbackEntry = new(
        Code: DefaultFallbackCode,
        Title: "Unhandled system error",
        UserMessage: "An unexpected error occurred. Please try again later.",
        SupportMessage: "Unhandled exception — check logs for correlation ID.",
        HttpStatus: 500,
        Severity: ErrorSeverity.Error,
        Retryable: false,
        Category: ErrorCategory.System);

    private readonly ImmutableDictionary<string, ErrorCatalogEntry> _entries;
    private readonly IReadOnlyList<ErrorCatalogEntry> _all;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryErrorCatalog"/>.
    /// </summary>
    /// <param name="entries">The error catalog entries to load.</param>
    public InMemoryErrorCatalog(IReadOnlyList<ErrorCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _all = entries;
        _entries = entries.ToImmutableDictionary(e => e.Code, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool TryGet(string code, out ErrorCatalogEntry? entry)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _entries.TryGetValue(code, out entry);
    }

    /// <inheritdoc />
    public ErrorCatalogEntry GetOrDefault(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (_entries.TryGetValue(code, out var entry))
            return entry;

        // Fall back to the default entry if it exists in the catalog,
        // otherwise use the hardcoded fallback.
        if (_entries.TryGetValue(DefaultFallbackCode, out var defaultEntry))
            return defaultEntry;

        return FallbackEntry;
    }

    /// <inheritdoc />
    public IReadOnlyList<ErrorCatalogEntry> All => _all;
}
