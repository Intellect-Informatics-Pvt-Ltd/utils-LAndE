using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// Fake implementation of <see cref="ICorrelationContextAccessor"/> with settable properties
/// for use in unit and integration tests.
/// </summary>
public sealed class FakeCorrelationContextAccessor : ICorrelationContextAccessor
{
    /// <inheritdoc />
    public string? CorrelationId { get; set; }

    /// <inheritdoc />
    public string? CausationId { get; set; }

    /// <inheritdoc />
    public string? TraceParent { get; set; }
}
