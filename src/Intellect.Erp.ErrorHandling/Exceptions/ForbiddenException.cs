using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents an authorization failure (HTTP 403).
/// </summary>
public class ForbiddenException : AppException
{
    /// <summary>Default error code for authorization failures.</summary>
    public const string DefaultCode = "ERP-CORE-SEC-0002";

    /// <summary>
    /// Initializes a new instance of <see cref="ForbiddenException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ForbiddenException(
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
