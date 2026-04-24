using System.Diagnostics;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Intellect.Erp.Observability.AspNetCore.Middleware;

/// <summary>
/// Middleware that logs HTTP request start and completion with duration,
/// status code, and optional body capture with redaction.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestLoggingMiddleware"/>.
    /// </summary>
    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request with start/end logging.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        IOptions<ObservabilityOptions> observabilityOptions,
        IRedactionEngine redactionEngine)
    {
        var options = observabilityOptions.Value.RequestLogging;
        var path = context.Request.Path.Value ?? "/";

        // Skip excluded paths
        if (IsExcludedPath(path, options.ExcludePaths))
        {
            await _next(context);
            return;
        }

        var httpMethod = context.Request.Method;
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;

        _logger.LogInformation(
            "HTTP {HttpMethod} {Path} started [CorrelationId: {CorrelationId}]",
            httpMethod, path, correlationId);

        // Optionally capture request body
        string? requestBody = null;
        if (options.CaptureRequestBody && IsWhitelistedPath(path, options.BodyWhitelist))
        {
            requestBody = await CaptureRequestBodyAsync(context.Request, redactionEngine);
        }

        var stopwatch = Stopwatch.StartNew();

        // Optionally capture response body
        Stream? originalBodyStream = null;
        MemoryStream? responseBodyStream = null;

        if (options.CaptureResponseBody && IsWhitelistedPath(path, options.BodyWhitelist))
        {
            originalBodyStream = context.Response.Body;
            responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            var route = context.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName
                        ?? context.Request.Path.Value;

            // Capture response body if enabled
            string? responseBody = null;
            if (responseBodyStream is not null && originalBodyStream is not null)
            {
                responseBody = await CaptureResponseBodyAsync(responseBodyStream, originalBodyStream, redactionEngine);
            }

            // Determine log level based on slow request threshold
            var isSlowRequest = durationMs >= options.SlowRequestThresholdMs;

            if (isSlowRequest)
            {
                _logger.LogWarning(
                    "HTTP {HttpMethod} {Path} responded {StatusCode} in {DurationMs:F1}ms (SLOW) [Route: {Route}] [CorrelationId: {CorrelationId}]",
                    httpMethod, path, statusCode, durationMs, route, correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {HttpMethod} {Path} responded {StatusCode} in {DurationMs:F1}ms [Route: {Route}] [CorrelationId: {CorrelationId}]",
                    httpMethod, path, statusCode, durationMs, route, correlationId);
            }

            if (requestBody is not null)
            {
                _logger.LogDebug("Request body: {RequestBody}", requestBody);
            }

            if (responseBody is not null)
            {
                _logger.LogDebug("Response body: {ResponseBody}", responseBody);
            }
        }
    }

    private static bool IsExcludedPath(string path, string[] excludePaths)
    {
        foreach (var excluded in excludePaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsWhitelistedPath(string path, string[] whitelist)
    {
        if (whitelist.Length == 0)
        {
            return false;
        }

        foreach (var allowed in whitelist)
        {
            if (path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static async Task<string?> CaptureRequestBodyAsync(HttpRequest request, IRedactionEngine redactionEngine)
    {
        try
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                return redactionEngine.Redact(body);
            }
        }
        catch
        {
            // Swallow — body capture must never break the pipeline
        }
        return null;
    }

    private static async Task<string?> CaptureResponseBodyAsync(
        MemoryStream responseBodyStream,
        Stream originalBodyStream,
        IRedactionEngine redactionEngine)
    {
        string? body = null;
        try
        {
            responseBodyStream.Position = 0;
            using var reader = new StreamReader(responseBodyStream);
            body = await reader.ReadToEndAsync();

            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalBodyStream);

            if (!string.IsNullOrWhiteSpace(body))
            {
                body = redactionEngine.Redact(body);
            }
        }
        catch
        {
            // Swallow — body capture must never break the pipeline
        }
        return body;
    }
}
