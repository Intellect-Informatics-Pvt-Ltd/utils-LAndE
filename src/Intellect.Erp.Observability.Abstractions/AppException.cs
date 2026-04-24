namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Abstract base exception for all application-level errors in the ePACS ERP system.
/// Carries a stable error code, category, severity, retryable flag, and correlation ID snapshot.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="AppException"/>.
    /// </summary>
    /// <param name="errorCode">Stable error code (e.g., <c>ERP-CORE-SYS-0001</c>).</param>
    /// <param name="category">The error category.</param>
    /// <param name="severity">The error severity.</param>
    /// <param name="retryable">Whether the operation can be retried.</param>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    protected AppException(
        string errorCode,
        ErrorCategory category,
        ErrorSeverity severity,
        bool retryable,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Category = category;
        Severity = severity;
        Retryable = retryable;
    }

    /// <summary>
    /// Gets the stable error code for this exception (e.g., <c>ERP-CORE-SYS-0001</c>).
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the error category used for HTTP status mapping and classification.
    /// </summary>
    public ErrorCategory Category { get; }

    /// <summary>
    /// Gets the severity level of this error.
    /// </summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// Gets a value indicating whether the failed operation can be retried.
    /// </summary>
    public bool Retryable { get; }

    /// <summary>
    /// Gets or sets the correlation ID snapshot captured at throw time.
    /// </summary>
    public string? CorrelationId { get; set; }
}
