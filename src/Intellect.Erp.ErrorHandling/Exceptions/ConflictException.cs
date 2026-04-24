using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents a state conflict with the current resource version (HTTP 409).
/// </summary>
public class ConflictException : AppException
{
    /// <summary>Default error code for conflict conditions.</summary>
    public const string DefaultCode = "ERP-CORE-CFL-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ConflictException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Conflict,
            ErrorSeverity.Warning,
            retryable: false,
            message,
            innerException)
    {
    }
}
