using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents an input validation failure (HTTP 400).
/// </summary>
public class ValidationException : AppException
{
    /// <summary>Default error code for validation failures.</summary>
    public const string DefaultCode = "ERP-CORE-VAL-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="fieldErrors">Optional array of field-level validation errors.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ValidationException(
        string message,
        FieldError[]? fieldErrors = null,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Validation,
            ErrorSeverity.Warning,
            retryable: false,
            message,
            innerException)
    {
        FieldErrors = fieldErrors ?? [];
    }

    /// <summary>
    /// Gets the field-level validation errors.
    /// </summary>
    public FieldError[] FieldErrors { get; }
}
