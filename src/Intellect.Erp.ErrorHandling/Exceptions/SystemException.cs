using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents an unclassified system-level failure (HTTP 500).
/// </summary>
public class SystemException : AppException
{
    /// <summary>Default error code for system failures.</summary>
    public const string DefaultCode = "ERP-CORE-SYS-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="SystemException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public SystemException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.System,
            ErrorSeverity.Error,
            retryable: false,
            message,
            innerException)
    {
    }
}
