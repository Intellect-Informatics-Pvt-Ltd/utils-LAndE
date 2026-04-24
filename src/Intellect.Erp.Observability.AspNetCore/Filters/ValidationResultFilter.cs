using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Intellect.Erp.Observability.AspNetCore.Filters;

/// <summary>
/// Action filter that checks <c>ModelState.IsValid</c> and converts invalid model state
/// into a <see cref="ErrorHandling.Exceptions.ValidationException"/> with <see cref="FieldError"/> array.
/// </summary>
public sealed class ValidationResultFilter : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var fieldErrors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(e => new FieldError(
                    Field: kvp.Key,
                    Code: "VALIDATION",
                    Message: e.ErrorMessage ?? e.Exception?.Message ?? "Invalid value.")))
                .ToArray();

            throw new ErrorHandling.Exceptions.ValidationException(
                "One or more validation errors occurred.",
                fieldErrors);
        }

        await next();
    }
}
