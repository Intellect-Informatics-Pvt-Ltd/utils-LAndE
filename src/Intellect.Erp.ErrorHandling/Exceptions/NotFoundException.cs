using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents a resource not found condition (HTTP 404).
/// </summary>
public class NotFoundException : AppException
{
    /// <summary>Default error code for not-found conditions.</summary>
    public const string DefaultCode = "ERP-CORE-NFD-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public NotFoundException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.NotFound,
            ErrorSeverity.Warning,
            retryable: false,
            message,
            innerException)
    {
    }
}
