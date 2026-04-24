using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents an optimistic concurrency conflict (HTTP 409).
/// Retryable by default. Optionally implements <see cref="ISagaCompensationException"/>
/// when wrapping a saga-scoped operation.
/// </summary>
public class ConcurrencyException : AppException, ISagaCompensationException
{
    /// <summary>Default error code for concurrency conflicts.</summary>
    public const string DefaultCode = "ERP-CORE-CON-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="ConcurrencyException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ConcurrencyException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Concurrency,
            ErrorSeverity.Warning,
            retryable: true,
            message,
            innerException)
    {
    }
}
