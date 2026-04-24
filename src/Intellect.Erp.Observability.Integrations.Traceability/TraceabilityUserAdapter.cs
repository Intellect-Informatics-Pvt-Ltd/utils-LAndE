using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Integrations.Traceability;

/// <summary>
/// Implements <see cref="IUserContextAccessor"/> by delegating to
/// <see cref="ITraceContextAccessor"/> from the Traceability utility.
/// </summary>
public sealed class TraceabilityUserAdapter : IUserContextAccessor
{
    private readonly ITraceContextAccessor _traceContext;

    /// <summary>
    /// Initializes a new instance of <see cref="TraceabilityUserAdapter"/>.
    /// </summary>
    /// <param name="traceContext">The Traceability context accessor to delegate to.</param>
    public TraceabilityUserAdapter(ITraceContextAccessor traceContext)
    {
        _traceContext = traceContext ?? throw new ArgumentNullException(nameof(traceContext));
    }

    /// <inheritdoc />
    public string? UserId => _traceContext.UserId;

    /// <inheritdoc />
    public string? UserName => _traceContext.UserName;

    /// <inheritdoc />
    public string? Role => _traceContext.Role;

    /// <inheritdoc />
    /// <remarks>
    /// ImpersonatingUserId is not available from the Traceability context; returns <c>null</c>.
    /// </remarks>
    public string? ImpersonatingUserId => null;
}
