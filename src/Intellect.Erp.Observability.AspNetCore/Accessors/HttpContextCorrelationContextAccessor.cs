using System.Diagnostics;
using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Intellect.Erp.Observability.AspNetCore.Accessors;

/// <summary>
/// <see cref="ICorrelationContextAccessor"/> backed by <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class HttpContextCorrelationContextAccessor : ICorrelationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpContextCorrelationContextAccessor"/>.
    /// </summary>
    public HttpContextCorrelationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();

    /// <inheritdoc />
    public string? CausationId =>
        _httpContextAccessor.HttpContext?.Items["CausationId"]?.ToString();

    /// <inheritdoc />
    public string? TraceParent
    {
        get
        {
            var activity = Activity.Current;
            if (activity is not null)
            {
                return $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}";
            }
            return null;
        }
    }
}
