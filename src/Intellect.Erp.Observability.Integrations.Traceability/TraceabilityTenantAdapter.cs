using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Integrations.Traceability;

/// <summary>
/// Implements <see cref="ITenantContextAccessor"/> by delegating to
/// <see cref="ITraceContextAccessor"/> from the Traceability utility.
/// </summary>
public sealed class TraceabilityTenantAdapter : ITenantContextAccessor
{
    private readonly ITraceContextAccessor _traceContext;

    /// <summary>
    /// Initializes a new instance of <see cref="TraceabilityTenantAdapter"/>.
    /// </summary>
    /// <param name="traceContext">The Traceability context accessor to delegate to.</param>
    public TraceabilityTenantAdapter(ITraceContextAccessor traceContext)
    {
        _traceContext = traceContext ?? throw new ArgumentNullException(nameof(traceContext));
    }

    /// <inheritdoc />
    public string? TenantId => _traceContext.TenantId;

    /// <inheritdoc />
    public string? StateCode => _traceContext.StateCode;

    /// <inheritdoc />
    /// <remarks>
    /// PacsId is not available from the Traceability context; returns <c>null</c>.
    /// </remarks>
    public string? PacsId => null;

    /// <inheritdoc />
    public string? BranchCode => _traceContext.BranchCode;
}
