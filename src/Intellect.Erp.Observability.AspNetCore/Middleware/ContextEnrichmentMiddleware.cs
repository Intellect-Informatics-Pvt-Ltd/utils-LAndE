using System.Security.Claims;
using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Intellect.Erp.Observability.AspNetCore.Middleware;

/// <summary>
/// Middleware that runs after authentication to extract user and tenant context
/// from claims and headers, storing them in <c>HttpContext.Items</c> and pushing
/// redacted values into the Serilog <see cref="LogContext"/>.
/// </summary>
public sealed class ContextEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    // HttpContext.Items keys
    internal const string UserIdKey = "UserId";
    internal const string UserNameKey = "UserName";
    internal const string RoleKey = "Role";
    internal const string ImpersonatingUserIdKey = "ImpersonatingUserId";
    internal const string TenantIdKey = "TenantId";
    internal const string StateCodeKey = "StateCode";
    internal const string PacsIdKey = "PacsId";
    internal const string BranchCodeKey = "BranchCode";

    /// <summary>
    /// Initializes a new instance of <see cref="ContextEnrichmentMiddleware"/>.
    /// </summary>
    public ContextEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Processes the HTTP request, extracting user and tenant context.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IRedactionEngine redactionEngine)
    {
        var user = context.User;
        var headers = context.Request.Headers;

        // Extract user context from claims
        var userId = GetClaimValue(user, ClaimTypes.NameIdentifier) ?? GetClaimValue(user, "sub");
        var userName = GetClaimValue(user, ClaimTypes.Name) ?? GetClaimValue(user, "name");
        var role = GetClaimValue(user, ClaimTypes.Role) ?? GetClaimValue(user, "role");
        var impersonatingUserId = GetClaimValue(user, "impersonating_user_id");

        // Extract tenant context from claims first, then fall back to headers
        var tenantId = GetClaimValue(user, "tenant_id") ?? GetHeaderValue(headers, "X-Tenant-Id");
        var stateCode = GetClaimValue(user, "state_code") ?? GetHeaderValue(headers, "X-State-Code");
        var pacsId = GetClaimValue(user, "pacs_id") ?? GetHeaderValue(headers, "X-Pacs-Id");
        var branchCode = GetClaimValue(user, "branch_code") ?? GetHeaderValue(headers, "X-Branch-Code");

        // Store in HttpContext.Items for accessor consumption
        SetItem(context, UserIdKey, userId);
        SetItem(context, UserNameKey, userName);
        SetItem(context, RoleKey, role);
        SetItem(context, ImpersonatingUserIdKey, impersonatingUserId);
        SetItem(context, TenantIdKey, tenantId);
        SetItem(context, StateCodeKey, stateCode);
        SetItem(context, PacsIdKey, pacsId);
        SetItem(context, BranchCodeKey, branchCode);

        // Push redacted values into LogContext
        var disposables = new List<IDisposable>();
        try
        {
            PushIfPresent(disposables, redactionEngine, UserIdKey, userId);
            PushIfPresent(disposables, redactionEngine, UserNameKey, userName);
            PushIfPresent(disposables, redactionEngine, RoleKey, role);
            PushIfPresent(disposables, redactionEngine, TenantIdKey, tenantId);
            PushIfPresent(disposables, redactionEngine, StateCodeKey, stateCode);
            PushIfPresent(disposables, redactionEngine, PacsIdKey, pacsId);
            PushIfPresent(disposables, redactionEngine, BranchCodeKey, branchCode);

            await _next(context);
        }
        finally
        {
            for (var i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
        }
    }

    private static void PushIfPresent(
        List<IDisposable> disposables,
        IRedactionEngine redactionEngine,
        string key,
        string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var redacted = redactionEngine.Redact(value);
            disposables.Add(LogContext.PushProperty(key, redacted));
        }
    }

    private static string? GetClaimValue(ClaimsPrincipal? user, string claimType)
    {
        var value = user?.FindFirst(claimType)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? GetHeaderValue(IHeaderDictionary headers, string headerName)
    {
        if (headers.TryGetValue(headerName, out var values))
        {
            var value = values.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    private static void SetItem(HttpContext context, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            context.Items[key] = value;
        }
    }
}
