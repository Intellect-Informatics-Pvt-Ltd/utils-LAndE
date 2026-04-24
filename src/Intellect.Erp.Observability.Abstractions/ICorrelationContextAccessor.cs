namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Provides access to the current request's correlation context for distributed tracing.
/// </summary>
public interface ICorrelationContextAccessor
{
    /// <summary>
    /// Gets the correlation ID for the current request scope.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the causation ID linking this request to its parent operation.
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Gets the W3C <c>traceparent</c> header value for the current trace.
    /// </summary>
    string? TraceParent { get; }
}
