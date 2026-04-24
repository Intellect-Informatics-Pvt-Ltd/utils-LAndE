using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents an external system failure (HTTP 502).
/// Not retryable by default (unlike <see cref="IntegrationException"/> which allows caller control).
/// </summary>
public class ExternalSystemException : AppException
{
    /// <summary>Default error code for external system failures.</summary>
    public const string DefaultCode = "ERP-CORE-INT-0002";

    /// <summary>
    /// Initializes a new instance of <see cref="ExternalSystemException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ExternalSystemException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Integration,
            ErrorSeverity.Error,
            retryable: false,
            message,
            innerException)
    {
    }
}
