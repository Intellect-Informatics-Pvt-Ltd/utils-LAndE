using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents a required dependency being unavailable (HTTP 503).
/// Retryable by default.
/// </summary>
public class DependencyException : AppException
{
    /// <summary>Default error code for dependency failures.</summary>
    public const string DefaultCode = "ERP-CORE-DEP-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="DependencyException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public DependencyException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Dependency,
            ErrorSeverity.Error,
            retryable: true,
            message,
            innerException)
    {
    }
}
