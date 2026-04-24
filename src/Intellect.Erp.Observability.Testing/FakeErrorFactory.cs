using Intellect.Erp.ErrorHandling;
using Intellect.Erp.ErrorHandling.Exceptions;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// Fake implementation of <see cref="IErrorFactory"/> that creates typed exceptions
/// without requiring DI. Uses a simple <see cref="InMemoryErrorCatalog"/> for
/// <see cref="FromCatalog"/> lookups.
/// </summary>
public sealed class FakeErrorFactory : IErrorFactory
{
    private readonly IErrorCatalog _catalog;
    private readonly string? _correlationId;

    /// <summary>
    /// Initializes a new instance of <see cref="FakeErrorFactory"/> with an optional
    /// correlation ID and error catalog entries.
    /// </summary>
    /// <param name="correlationId">Optional correlation ID to stamp on created exceptions.</param>
    /// <param name="catalogEntries">Optional catalog entries for <see cref="FromCatalog"/> lookups.</param>
    public FakeErrorFactory(string? correlationId = null, IReadOnlyList<ErrorCatalogEntry>? catalogEntries = null)
    {
        _correlationId = correlationId;
        _catalog = new InMemoryErrorCatalog(catalogEntries ?? []);
    }

    /// <inheritdoc />
    public AppException Validation(string message, FieldError[]? fieldErrors = null, Exception? innerException = null)
        => Stamp(new ValidationException(message, fieldErrors, innerException: innerException));

    /// <inheritdoc />
    public AppException BusinessRule(string message, Exception? innerException = null)
        => Stamp(new BusinessRuleException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException NotFound(string message, Exception? innerException = null)
        => Stamp(new NotFoundException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException Conflict(string message, Exception? innerException = null)
        => Stamp(new ConflictException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException Unauthorized(string message, Exception? innerException = null)
        => Stamp(new UnauthorizedException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException Forbidden(string message, Exception? innerException = null)
        => Stamp(new ForbiddenException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException Integration(string message, bool retryable = false, Exception? innerException = null)
        => Stamp(new IntegrationException(message, retryable, innerException: innerException));

    /// <inheritdoc />
    public AppException Dependency(string message, Exception? innerException = null)
        => Stamp(new DependencyException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException DataIntegrity(string message, Exception? innerException = null)
        => Stamp(new DataIntegrityException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException Concurrency(string message, Exception? innerException = null)
        => Stamp(new ConcurrencyException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException ExternalSystem(string message, Exception? innerException = null)
        => Stamp(new ExternalSystemException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException System(string message, Exception? innerException = null)
        => Stamp(new ErrorHandling.Exceptions.SystemException(message, innerException: innerException));

    /// <inheritdoc />
    public AppException FromCatalog(string errorCode, string? message = null, Exception? innerException = null)
    {
        var entry = _catalog.GetOrDefault(errorCode);
        var msg = message ?? entry.UserMessage;

        AppException exception = entry.Category switch
        {
            ErrorCategory.Validation => new ValidationException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.Business => new BusinessRuleException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.NotFound => new NotFoundException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.Conflict => new ConflictException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.Security => new UnauthorizedException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.Integration => new IntegrationException(msg, entry.Retryable, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.Dependency => new DependencyException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.Data => new DataIntegrityException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.Concurrency => new ConcurrencyException(msg, errorCode: entry.Code, innerException: innerException),
            ErrorCategory.System => new ErrorHandling.Exceptions.SystemException(msg, errorCode: entry.Code, innerException: innerException),
            _ => new ErrorHandling.Exceptions.SystemException(msg, errorCode: entry.Code, innerException: innerException),
        };

        return Stamp(exception);
    }

    private AppException Stamp(AppException exception)
    {
        exception.CorrelationId = _correlationId;
        return exception;
    }
}
