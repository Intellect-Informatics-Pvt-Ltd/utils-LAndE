namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Standardized JSON error envelope returned to API consumers.
/// Compatible with RFC 7807 ProblemDetails plus ePACS extensions.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>Always <c>false</c> for error responses.</summary>
    public bool Success { get; init; }

    /// <summary>Stable error code from the error catalog (e.g., <c>ERP-CORE-SYS-0001</c>).</summary>
    public required string ErrorCode { get; init; }

    /// <summary>Short, human-readable title for the error.</summary>
    public required string Title { get; init; }

    /// <summary>Consumer-safe error message.</summary>
    public required string Message { get; init; }

    /// <summary>The correlation ID for the request that produced this error.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The OpenTelemetry trace ID, when available.</summary>
    public string? TraceId { get; init; }

    /// <summary>HTTP status code.</summary>
    public int Status { get; init; }

    /// <summary>Error severity level as a string (e.g., "Error", "Critical").</summary>
    public required string Severity { get; init; }

    /// <summary>Whether the failed operation can be retried.</summary>
    public bool Retryable { get; init; }

    /// <summary>ISO-8601 UTC timestamp of when the error occurred.</summary>
    public required string Timestamp { get; init; }

    /// <summary>RFC 7807 <c>type</c> URI for the error code.</summary>
    public string? Type { get; init; }

    /// <summary>Field-level validation errors, present when the error is a validation failure.</summary>
    public FieldError[]? FieldErrors { get; init; }

    /// <summary>The .NET exception type name. Suppressed in Production.</summary>
    public string? ExceptionType { get; init; }

    /// <summary>The exception stack trace. Suppressed in Production.</summary>
    public string? StackTrace { get; init; }

    /// <summary>A support reference identifier for escalation. Suppressed in Production.</summary>
    public string? SupportReference { get; init; }
}
