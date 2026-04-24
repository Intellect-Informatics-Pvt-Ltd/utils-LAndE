using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Intellect.Erp.ErrorHandling;
using Intellect.Erp.ErrorHandling.Exceptions;
using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Intellect.Erp.Observability.AspNetCore.Middleware;

/// <summary>
/// Middleware that catches unhandled exceptions and maps them to standardized
/// <see cref="ErrorResponse"/> JSON bodies with appropriate HTTP status codes.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of <see cref="GlobalExceptionMiddleware"/>.
    /// </summary>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Processes the HTTP request and catches any unhandled exceptions.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IOptions<ErrorHandlingOptions> errorOptions)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorOptions.Value);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, ErrorHandlingOptions options)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var isProduction = _environment.IsProduction();

        // Refuse to enable exception details in Production
        var includeDetails = options.IncludeExceptionDetailsInResponse;
        if (includeDetails && isProduction)
        {
            _logger.LogWarning(
                "IncludeExceptionDetailsInResponse is enabled but environment is Production. " +
                "Exception details will NOT be included in responses.");
            includeDetails = false;
        }

        // Convert FluentValidation.ValidationException by type name
        if (exception.GetType().FullName == "FluentValidation.ValidationException")
        {
            exception = ConvertFluentValidationException(exception);
        }

        var (statusCode, errorResponse) = MapException(exception, correlationId, options, includeDetails);

        // Emit single structured error log
        _logger.LogError(
            exception,
            "Unhandled exception {ErrorCode} for correlation {CorrelationId}: {ErrorMessage}",
            errorResponse.ErrorCode,
            correlationId,
            exception.Message);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    private static (int StatusCode, ErrorResponse Response) MapException(
        Exception exception,
        string correlationId,
        ErrorHandlingOptions options,
        bool includeDetails)
    {
        int statusCode;
        string errorCode;
        string title;
        string message;
        string severity;
        bool retryable;
        FieldError[]? fieldErrors = null;

        switch (exception)
        {
            case ValidationException validationEx:
                statusCode = 400;
                errorCode = validationEx.ErrorCode;
                title = "Validation Error";
                message = validationEx.Message;
                severity = validationEx.Severity.ToString();
                retryable = validationEx.Retryable;
                fieldErrors = validationEx.FieldErrors;
                break;

            case BusinessRuleException bizEx:
                statusCode = 422;
                errorCode = bizEx.ErrorCode;
                title = "Business Rule Violation";
                message = bizEx.Message;
                severity = bizEx.Severity.ToString();
                retryable = bizEx.Retryable;
                break;

            case NotFoundException nfEx:
                statusCode = 404;
                errorCode = nfEx.ErrorCode;
                title = "Not Found";
                message = nfEx.Message;
                severity = nfEx.Severity.ToString();
                retryable = nfEx.Retryable;
                break;

            case ConflictException conflictEx:
                statusCode = 409;
                errorCode = conflictEx.ErrorCode;
                title = "Conflict";
                message = conflictEx.Message;
                severity = conflictEx.Severity.ToString();
                retryable = conflictEx.Retryable;
                break;

            case UnauthorizedException unauthEx:
                statusCode = 401;
                errorCode = unauthEx.ErrorCode;
                title = "Unauthorized";
                message = unauthEx.Message;
                severity = unauthEx.Severity.ToString();
                retryable = unauthEx.Retryable;
                break;

            case ForbiddenException forbiddenEx:
                statusCode = 403;
                errorCode = forbiddenEx.ErrorCode;
                title = "Forbidden";
                message = forbiddenEx.Message;
                severity = forbiddenEx.Severity.ToString();
                retryable = forbiddenEx.Retryable;
                break;

            case ConcurrencyException concurrencyEx:
                statusCode = 409;
                errorCode = concurrencyEx.ErrorCode;
                title = "Concurrency Conflict";
                message = concurrencyEx.Message;
                severity = concurrencyEx.Severity.ToString();
                retryable = concurrencyEx.Retryable;
                break;

            case DataIntegrityException dataEx:
                statusCode = 500;
                errorCode = dataEx.ErrorCode;
                title = "Data Integrity Error";
                message = dataEx.Message;
                severity = dataEx.Severity.ToString();
                retryable = dataEx.Retryable;
                break;

            case IntegrationException integrationEx:
                statusCode = 502;
                errorCode = integrationEx.ErrorCode;
                title = "Integration Error";
                message = integrationEx.Message;
                severity = integrationEx.Severity.ToString();
                retryable = integrationEx.Retryable;
                break;

            case DependencyException depEx:
                statusCode = 503;
                errorCode = depEx.ErrorCode;
                title = "Dependency Unavailable";
                message = depEx.Message;
                severity = depEx.Severity.ToString();
                retryable = depEx.Retryable;
                break;

            case ExternalSystemException extEx:
                statusCode = 502;
                errorCode = extEx.ErrorCode;
                title = "External System Error";
                message = extEx.Message;
                severity = extEx.Severity.ToString();
                retryable = extEx.Retryable;
                break;

            case ErrorHandling.Exceptions.SystemException sysEx:
                statusCode = 500;
                errorCode = sysEx.ErrorCode;
                title = "System Error";
                message = sysEx.Message;
                severity = sysEx.Severity.ToString();
                retryable = sysEx.Retryable;
                break;

            // Catch any other AppException subclass not explicitly listed
            case AppException appEx:
                statusCode = 500;
                errorCode = appEx.ErrorCode;
                title = "Application Error";
                message = appEx.Message;
                severity = appEx.Severity.ToString();
                retryable = appEx.Retryable;
                break;

            case TaskCanceledException:
            case OperationCanceledException:
                statusCode = 499;
                errorCode = "ERP-CORE-SYS-0002";
                title = "Request Cancelled";
                message = "The request was cancelled.";
                severity = ErrorSeverity.Warning.ToString();
                retryable = true;
                break;

            default:
                statusCode = 500;
                errorCode = "ERP-CORE-SYS-0001";
                title = "Internal Server Error";
                message = "An unexpected error occurred. Please try again later.";
                severity = ErrorSeverity.Error.ToString();
                retryable = false;
                break;
        }

        var traceId = Activity.Current?.TraceId.ToString();

        var response = new ErrorResponse
        {
            Success = false,
            ErrorCode = errorCode,
            Title = title,
            Message = message,
            CorrelationId = correlationId,
            TraceId = traceId,
            Status = statusCode,
            Severity = severity,
            Retryable = retryable,
            Timestamp = DateTimeOffset.UtcNow.ToString("o"),
            Type = $"{options.ClientErrorUriBase}{errorCode}",
            FieldErrors = fieldErrors,
            ExceptionType = includeDetails ? exception.GetType().FullName : null,
            StackTrace = includeDetails ? exception.StackTrace : null,
            SupportReference = includeDetails ? correlationId : null
        };

        return (statusCode, response);
    }

    private static ValidationException ConvertFluentValidationException(Exception fluentException)
    {
        // Use reflection to extract Errors from FluentValidation.ValidationException
        var errorsProperty = fluentException.GetType().GetProperty("Errors");
        var fieldErrors = new List<FieldError>();

        if (errorsProperty?.GetValue(fluentException) is System.Collections.IEnumerable errors)
        {
            foreach (var error in errors)
            {
                var errorType = error.GetType();
                var propertyName = errorType.GetProperty("PropertyName")?.GetValue(error)?.ToString() ?? string.Empty;
                var errorMessage = errorType.GetProperty("ErrorMessage")?.GetValue(error)?.ToString() ?? string.Empty;
                var errorCode = errorType.GetProperty("ErrorCode")?.GetValue(error)?.ToString() ?? "VALIDATION";

                fieldErrors.Add(new FieldError(propertyName, errorCode, errorMessage));
            }
        }

        return new ValidationException(
            fluentException.Message,
            fieldErrors.ToArray(),
            innerException: fluentException);
    }
}
