using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Represents a business rule violation (HTTP 422).
/// Implements <see cref="IDomainPolicyRejectionException"/> for audit outcome mapping.
/// </summary>
public class BusinessRuleException : AppException, IDomainPolicyRejectionException
{
    /// <summary>Default error code for business rule violations.</summary>
    public const string DefaultCode = "ERP-CORE-BIZ-0001";

    /// <summary>
    /// Initializes a new instance of <see cref="BusinessRuleException"/>.
    /// </summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="errorCode">Optional error code override.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public BusinessRuleException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(
            errorCode ?? DefaultCode,
            ErrorCategory.Business,
            ErrorSeverity.Warning,
            retryable: false,
            message,
            innerException)
    {
    }
}
