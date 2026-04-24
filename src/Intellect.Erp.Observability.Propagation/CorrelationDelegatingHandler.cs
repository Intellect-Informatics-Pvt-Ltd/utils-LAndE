using System.Diagnostics;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Propagation;

/// <summary>
/// Delegating handler that propagates correlation context on outbound HTTP requests.
/// Sets <c>X-Correlation-Id</c>, <c>traceparent</c> (W3C format), and optional
/// <c>X-Causation-Id</c>, <c>X-Tenant</c>, and <c>X-State-Code</c> headers.
/// </summary>
public sealed class CorrelationDelegatingHandler : DelegatingHandler
{
    private readonly ICorrelationContextAccessor _correlationAccessor;
    private readonly ITenantContextAccessor? _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="CorrelationDelegatingHandler"/>.
    /// </summary>
    /// <param name="correlationAccessor">Provides the current correlation context.</param>
    /// <param name="tenantAccessor">Optional tenant context accessor for tenant/state headers.</param>
    public CorrelationDelegatingHandler(
        ICorrelationContextAccessor correlationAccessor,
        ITenantContextAccessor? tenantAccessor = null)
    {
        _correlationAccessor = correlationAccessor ?? throw new ArgumentNullException(nameof(correlationAccessor));
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Set X-Correlation-Id from accessor
        var correlationId = _correlationAccessor.CorrelationId;
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        // Set traceparent in W3C format from Activity.Current when present
        var activity = Activity.Current;
        if (activity is not null)
        {
            var traceId = activity.TraceId.ToHexString();
            var spanId = activity.SpanId.ToHexString();
            var flags = ((int)activity.ActivityTraceFlags).ToString("x2");
            var traceparent = $"00-{traceId}-{spanId}-{flags}";
            request.Headers.TryAddWithoutValidation("traceparent", traceparent);
        }

        // Optionally set X-Causation-Id
        var causationId = _correlationAccessor.CausationId;
        if (!string.IsNullOrEmpty(causationId))
        {
            request.Headers.TryAddWithoutValidation("X-Causation-Id", causationId);
        }

        // Optionally set X-Tenant
        var tenantId = _tenantAccessor?.TenantId;
        if (!string.IsNullOrEmpty(tenantId))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant", tenantId);
        }

        // Optionally set X-State-Code
        var stateCode = _tenantAccessor?.StateCode;
        if (!string.IsNullOrEmpty(stateCode))
        {
            request.Headers.TryAddWithoutValidation("X-State-Code", stateCode);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
