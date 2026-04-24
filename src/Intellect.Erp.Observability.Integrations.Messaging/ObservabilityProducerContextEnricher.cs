using System.Diagnostics;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Integrations.Messaging;

/// <summary>
/// Implements <see cref="IProducerContextEnricher"/> to enrich Kafka <see cref="EventEnvelope"/>
/// instances with correlation, user, and tenant context from the Observability platform.
/// Formats the <c>traceparent</c> header in W3C format rather than using <c>Activity.Current.Id</c> directly.
/// </summary>
public sealed class ObservabilityProducerContextEnricher : IProducerContextEnricher
{
    private readonly ICorrelationContextAccessor _correlationAccessor;
    private readonly IUserContextAccessor _userAccessor;
    private readonly ITenantContextAccessor _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="ObservabilityProducerContextEnricher"/>.
    /// </summary>
    /// <param name="correlationAccessor">Provides the current correlation context.</param>
    /// <param name="userAccessor">Provides the current user context.</param>
    /// <param name="tenantAccessor">Provides the current tenant context.</param>
    public ObservabilityProducerContextEnricher(
        ICorrelationContextAccessor correlationAccessor,
        IUserContextAccessor userAccessor,
        ITenantContextAccessor tenantAccessor)
    {
        _correlationAccessor = correlationAccessor ?? throw new ArgumentNullException(nameof(correlationAccessor));
        _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
    }

    /// <inheritdoc />
    public void Enrich(EventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        // Enrich correlation fields
        var correlationId = _correlationAccessor.CorrelationId;
        if (!string.IsNullOrEmpty(correlationId))
        {
            envelope.CorrelationId = correlationId;
        }

        var causationId = _correlationAccessor.CausationId;
        if (!string.IsNullOrEmpty(causationId))
        {
            envelope.CausationId = causationId;
        }

        // Format traceparent in W3C format from Activity.Current
        var activity = Activity.Current;
        if (activity is not null)
        {
            var traceId = activity.TraceId.ToHexString();
            var spanId = activity.SpanId.ToHexString();
            var flags = ((int)activity.ActivityTraceFlags).ToString("x2");
            envelope.TraceParent = $"00-{traceId}-{spanId}-{flags}";
        }

        // Enrich user context
        var userId = _userAccessor.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            envelope.UserId = userId;
        }

        // Enrich tenant context
        var tenantId = _tenantAccessor.TenantId;
        if (!string.IsNullOrEmpty(tenantId))
        {
            envelope.TenantId = tenantId;
        }
    }
}
