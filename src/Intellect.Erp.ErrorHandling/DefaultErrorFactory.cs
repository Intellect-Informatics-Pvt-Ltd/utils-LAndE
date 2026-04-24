using Intellect.Erp.ErrorHandling.Exceptions;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling;

/// <summary>
/// Default implementation of <see cref="IErrorFactory"/> that creates typed
/// <see cref="AppException"/> instances and stamps the current correlation ID.
/// </summary>
public sealed class DefaultErrorFactory : IErrorFactory
{
    private readonly ICorrelationContextAccessor _correlationAccessor;
    private readonly IErrorCatalog _catalog;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultErrorFactory"/>.
    /// </summary>
    /// <param name="correlationAccessor">Provides the current correlation ID.</param>
    /// <param name="catalog">The error catalog for <see cref="FromCatalog"/> lookups.</param>
    public DefaultErrorFactory(
        ICorrelationContextAccessor correlationAccessor,
        IErrorCatalog catalog)
    {
        _correlationAccessor = correlationAccessor ?? throw new ArgumentNullException(nameof(correlationAccessor));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
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
        => Stamp(new Exceptions.SystemException(message, innerException: innerException));

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
            ErrorCategory.System => new Exceptions.SystemException(msg, errorCode: entry.Code, innerException: innerException),
            _ => new Exceptions.SystemException(msg, errorCode: entry.Code, innerException: innerException),
        };

        return Stamp(exception);
    }

    private AppException Stamp(AppException exception)
    {
        exception.CorrelationId = _correlationAccessor.CorrelationId;
        return exception;
    }
}
