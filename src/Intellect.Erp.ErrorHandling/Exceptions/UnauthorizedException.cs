using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents an authentication failure (HTTP 401).
/// </summary>
public class UnauthorizedException : AppException
{
    /// <summary>Default error code for authentication failures.</summary>
    public const string DefaultCode = "ERP-CORE-SEC-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="UnauthorizedException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public UnauthorizedException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Security,
            ErrorSeverity.Warning,
            retryable: false,
            message,
            innerException)
    {
    }
}
