using Intellect.Erp.Observability.AspNetCore.Internal;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Intellect.Erp.Observability.AspNetCore.Middleware;

/// <summary>
/// Middleware that reads or generates a correlation ID for each request,
/// stores it in <c>HttpContext.Items["CorrelationId"]</c>, pushes it into
/// the Serilog <see cref="LogContext"/>, and echoes it on the response header.
/// </summary>
public sealed class CorrelationMiddleware
{
    private const string CorrelationIdKey = "CorrelationId";
    private const string ResponseHeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly bool _isPassthrough;

    /// <summary>
    /// Initializes a new instance of <see cref="CorrelationMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="isPassthrough">
    /// When <c>true</c>, the middleware acts as a no-op passthrough
    /// (used when Traceability middleware is already registered).
    /// </param>
    public CorrelationMiddleware(RequestDelegate next, bool isPassthrough = false)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _isPassthrough = isPassthrough;
    }

    /// <summary>
    /// Processes the HTTP request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (_isPassthrough)
        {
            await _next(context);
            return;
        }

        var correlationId = ResolveCorrelationId(context.Request);

        context.Items[CorrelationIdKey] = correlationId;

        // Echo correlation ID on response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ResponseHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(CorrelationIdKey, correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpRequest request)
    {
        // Check X-Correlation-Id (case-insensitive header lookup covers both spellings)
        if (request.Headers.TryGetValue("X-Correlation-Id", out var values) && !string.IsNullOrWhiteSpace(values.ToString()))
        {
            return values.ToString();
        }

        if (request.Headers.TryGetValue("X-Correlation-ID", out values) && !string.IsNullOrWhiteSpace(values.ToString()))
        {
            return values.ToString();
        }

        // Fall back to traceparent header extraction
        if (request.Headers.TryGetValue("traceparent", out values) && !string.IsNullOrWhiteSpace(values.ToString()))
        {
            var traceparent = values.ToString();
            // W3C format: 00-{traceId}-{spanId}-{flags}
            var parts = traceparent.Split('-');
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                return parts[1];
            }
        }

        // Generate a new ULID-26
        return UlidGenerator.NewUlid();
    }
}
