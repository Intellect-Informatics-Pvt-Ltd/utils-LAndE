namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Provides access to the current tenant context for multi-tenant log enrichment.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets the state code for the current tenant.
    /// </summary>
    string? StateCode { get; }

    /// <summary>
    /// Gets the PACS (Primary Agricultural Credit Society) identifier.
    /// </summary>
    string? PacsId { get; }

    /// <summary>
    /// Gets the branch code for the current tenant.
    /// </summary>
    string? BranchCode { get; }
}
