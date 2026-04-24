using System.Security.Claims;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;

namespace Intellect.Erp.Observability.AspNetCore.Accessors;

/// <summary>
/// <see cref="IUserContextAccessor"/> backed by <see cref="HttpContext.User"/> claims
/// and <see cref="HttpContext.Items"/> populated by <see cref="ContextEnrichmentMiddleware"/>.
/// </summary>
public sealed class HttpContextUserContextAccessor : IUserContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpContextUserContextAccessor"/>.
    /// </summary>
    public HttpContextUserContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public string? UserId =>
        GetItem(ContextEnrichmentMiddleware.UserIdKey)
        ?? GetClaim(ClaimTypes.NameIdentifier)
        ?? GetClaim("sub");

    /// <inheritdoc />
    public string? UserName =>
        GetItem(ContextEnrichmentMiddleware.UserNameKey)
        ?? GetClaim(ClaimTypes.Name)
        ?? GetClaim("name");

    /// <inheritdoc />
    public string? Role =>
        GetItem(ContextEnrichmentMiddleware.RoleKey)
        ?? GetClaim(ClaimTypes.Role)
        ?? GetClaim("role");

    /// <inheritdoc />
    public string? ImpersonatingUserId =>
        GetItem(ContextEnrichmentMiddleware.ImpersonatingUserIdKey)
        ?? GetClaim("impersonating_user_id");

    private string? GetItem(string key)
    {
        return _httpContextAccessor.HttpContext?.Items[key]?.ToString();
    }

    private string? GetClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
