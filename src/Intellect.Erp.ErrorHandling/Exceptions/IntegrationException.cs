using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents an external integration or upstream service failure (HTTP 502).
/// The retryable flag is determined by the caller.
/// </summary>
public class IntegrationException : AppException
{
    /// <summary>Default error code for integration failures.</summary>
    public const string DefaultCode = "ERP-CORE-INT-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="IntegrationException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="retryable">Whether the integration call can be retried.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public IntegrationException(
        string message,
        bool retryable = false,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Integration,
            ErrorSeverity.Error,
            retryable,
            message,
            innerException)
    {
    }
}
