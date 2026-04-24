using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Integrations.Traceability;

/// <summary>
/// Implements <see cref="ICorrelationContextAccessor"/> by delegating to
/// <see cref="ITraceContextAccessor"/> from the Traceability utility.
/// </summary>
public sealed class TraceabilityCorrelationAdapter : ICorrelationContextAccessor
{
    private readonly ITraceContextAccessor _traceContext;

    /// <summary>
    /// Initializes a new instance of <see cref="TraceabilityCorrelationAdapter"/>.
    /// </summary>
    /// <param name="traceContext">The Traceability context accessor to delegate to.</param>
    public TraceabilityCorrelationAdapter(ITraceContextAccessor traceContext)
    {
        _traceContext = traceContext ?? throw new ArgumentNullException(nameof(traceContext));
    }

    /// <inheritdoc />
    public string? CorrelationId => _traceContext.CorrelationId;

    /// <inheritdoc />
    /// <remarks>
    /// CausationId is not available from the Traceability context; returns <c>null</c>.
    /// </remarks>
    public string? CausationId => null;

    /// <inheritdoc />
    /// <remarks>
    /// TraceParent is not available from the Traceability context; returns <c>null</c>.
    /// </remarks>
    public string? TraceParent => null;
}
