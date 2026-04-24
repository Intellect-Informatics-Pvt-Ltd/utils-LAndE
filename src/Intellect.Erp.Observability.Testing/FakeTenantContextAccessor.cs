using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// Fake implementation of <see cref="ITenantContextAccessor"/> with settable properties
/// for use in unit and integration tests.
/// </summary>
public sealed class FakeTenantContextAccessor : ITenantContextAccessor
{
    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <inheritdoc />
    public string? StateCode { get; set; }

    /// <inheritdoc />
    public string? PacsId { get; set; }

    /// <inheritdoc />
    public string? BranchCode { get; set; }
}
