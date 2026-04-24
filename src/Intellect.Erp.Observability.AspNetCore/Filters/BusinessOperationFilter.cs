using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog.Context;

namespace Intellect.Erp.Observability.AspNetCore.Filters;

/// <summary>
/// Action filter that reads the <see cref="BusinessOperationAttribute"/> from the action
/// and pushes module, feature, and operation into the Serilog <see cref="LogContext"/> scope.
/// </summary>
public sealed class BusinessOperationFilter : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = GetBusinessOperationAttribute(context);

        if (attribute is null)
        {
            await next();
            return;
        }

        using var moduleScope = LogContext.PushProperty("Module", attribute.Module);
        using var featureScope = LogContext.PushProperty("Feature", attribute.Feature);
        using var operationScope = LogContext.PushProperty("Operation", attribute.Operation);

        await next();
    }

    private static BusinessOperationAttribute? GetBusinessOperationAttribute(ActionExecutingContext context)
    {
        // Check action method first
        var actionDescriptor = context.ActionDescriptor;

        if (actionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerAction)
        {
            var attr = controllerAction.MethodInfo
                .GetCustomAttributes(typeof(BusinessOperationAttribute), true)
                .FirstOrDefault() as BusinessOperationAttribute;

            if (attr is not null)
                return attr;

            // Fall back to controller class
            attr = controllerAction.ControllerTypeInfo
                .GetCustomAttributes(typeof(BusinessOperationAttribute), true)
                .FirstOrDefault() as BusinessOperationAttribute;

            return attr;
        }

        return null;
    }
}
