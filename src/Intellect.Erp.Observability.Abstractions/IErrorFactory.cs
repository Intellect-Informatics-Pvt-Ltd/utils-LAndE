namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Factory for creating typed <see cref="AppException"/> instances.
/// Automatically stamps the current correlation ID on each exception at creation time.
/// </summary>
public interface IErrorFactory
{
    /// <summary>Creates a <c>ValidationException</c> with the specified message and optional field errors.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="fieldErrors">Optional array of field-level validation errors.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing a validation failure.</returns>
    AppException Validation(string message, FieldError[]? fieldErrors = null, Exception? innerException = null);

    /// <summary>Creates a <c>BusinessRuleException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing a business rule violation.</returns>
    AppException BusinessRule(string message, Exception? innerException = null);

    /// <summary>Creates a <c>NotFoundException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing a not-found condition.</returns>
    AppException NotFound(string message, Exception? innerException = null);

    /// <summary>Creates a <c>ConflictException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing a state conflict.</returns>
    AppException Conflict(string message, Exception? innerException = null);

    /// <summary>Creates an <c>UnauthorizedException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing an authentication failure.</returns>
    AppException Unauthorized(string message, Exception? innerException = null);

    /// <summary>Creates a <c>ForbiddenException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing an authorization failure.</returns>
    AppException Forbidden(string message, Exception? innerException = null);

    /// <summary>Creates an <c>IntegrationException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="retryable">Whether the integration call can be retried.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing an integration failure.</returns>
    AppException Integration(string message, bool retryable = false, Exception? innerException = null);

    /// <summary>Creates a <c>DependencyException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing a dependency failure.</returns>
    AppException Dependency(string message, Exception? innerException = null);

    /// <summary>Creates a <c>DataIntegrityException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing a data integrity failure.</returns>
    AppException DataIntegrity(string message, Exception? innerException = null);

    /// <summary>Creates a <c>ConcurrencyException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing a concurrency conflict.</returns>
    AppException Concurrency(string message, Exception? innerException = null);

    /// <summary>Creates an <c>ExternalSystemException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing an external system failure.</returns>
    AppException ExternalSystem(string message, Exception? innerException = null);

    /// <summary>Creates a <c>SystemException</c>.</summary>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> representing an unclassified system failure.</returns>
    AppException System(string message, Exception? innerException = null);

    /// <summary>
    /// Creates an <see cref="AppException"/> from a catalog entry identified by error code.
    /// Falls back to <c>ERP-CORE-SYS-0001</c> if the code is not found.
    /// </summary>
    /// <param name="errorCode">The error code to look up in the catalog.</param>
    /// <param name="message">Optional override message. Uses the catalog entry's user message if null.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An <see cref="AppException"/> populated from the catalog entry.</returns>
    AppException FromCatalog(string errorCode, string? message = null, Exception? innerException = null);
}
