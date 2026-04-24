using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;

namespace Intellect.Erp.Observability.AspNetCore.Accessors;

/// <summary>
/// <see cref="ITenantContextAccessor"/> backed by <see cref="HttpContext.Items"/>
/// populated by <see cref="ContextEnrichmentMiddleware"/>, with fallback to custom headers.
/// </summary>
public sealed class HttpContextTenantContextAccessor : ITenantContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpContextTenantContextAccessor"/>.
    /// </summary>
    public HttpContextTenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public string? TenantId =>
        GetItem(ContextEnrichmentMiddleware.TenantIdKey)
        ?? GetHeader("X-Tenant-Id");

    /// <inheritdoc />
    public string? StateCode =>
        GetItem(ContextEnrichmentMiddleware.StateCodeKey)
        ?? GetHeader("X-State-Code");

    /// <inheritdoc />
    public string? PacsId =>
        GetItem(ContextEnrichmentMiddleware.PacsIdKey)
        ?? GetHeader("X-Pacs-Id");

    /// <inheritdoc />
    public string? BranchCode =>
        GetItem(ContextEnrichmentMiddleware.BranchCodeKey)
        ?? GetHeader("X-Branch-Code");

    private string? GetItem(string key)
    {
        return _httpContextAccessor.HttpContext?.Items[key]?.ToString();
    }

    private string? GetHeader(string headerName)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return null;

        if (context.Request.Headers.TryGetValue(headerName, out var values))
        {
            var value = values.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }
}
