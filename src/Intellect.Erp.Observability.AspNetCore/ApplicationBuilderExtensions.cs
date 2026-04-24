using Intellect.Erp.Observability.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Intellect.Erp.Observability.AspNetCore;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to register the observability middleware pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Marker key set on <see cref="IApplicationBuilder.Properties"/> after observability
    /// middlewares are registered.
    /// </summary>
    public const string ObservabilityRegisteredMarker = "Observability.Middleware.Registered";

    /// <summary>
    /// Marker key checked on <see cref="IApplicationBuilder.Properties"/> to detect
    /// whether the Traceability middleware is already registered.
    /// </summary>
    public const string TraceabilityRegisteredMarker = "Traceability.Middleware.Registered";

    /// <summary>
    /// Registers the observability middleware pipeline in the documented order:
    /// <list type="number">
    ///   <item><see cref="CorrelationMiddleware"/></item>
    ///   <item><see cref="GlobalExceptionMiddleware"/></item>
    ///   <item><see cref="ContextEnrichmentMiddleware"/></item>
    ///   <item><see cref="RequestLoggingMiddleware"/></item>
    /// </list>
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Detect Traceability middleware
        var traceabilityDetected = app.Properties.ContainsKey(TraceabilityRegisteredMarker);

        // 1. CorrelationMiddleware — passthrough if Traceability is detected
        app.UseMiddleware<CorrelationMiddleware>(traceabilityDetected);

        // 2. GlobalExceptionMiddleware
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // 3. ContextEnrichmentMiddleware (runs after auth in the pipeline)
        app.UseMiddleware<ContextEnrichmentMiddleware>();

        // 4. RequestLoggingMiddleware
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Set marker property
        app.Properties[ObservabilityRegisteredMarker] = true;

        return app;
    }
}
