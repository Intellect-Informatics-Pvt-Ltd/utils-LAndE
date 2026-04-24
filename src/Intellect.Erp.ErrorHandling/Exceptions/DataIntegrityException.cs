using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents a data integrity or persistence failure (HTTP 500).
/// </summary>
public class DataIntegrityException : AppException
{
    /// <summary>Default error code for data integrity failures.</summary>
    public const string DefaultCode = "ERP-CORE-DAT-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="DataIntegrityException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public DataIntegrityException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Data,
            ErrorSeverity.Error,
            retryable: false,
            message,
            innerException)
    {
    }
}
