namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Provides access to the authenticated user's context for log enrichment and audit.
/// </summary>
public interface IUserContextAccessor
{
    /// <summary>
    /// Gets the authenticated user's identifier.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the authenticated user's display name.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the authenticated user's role.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Gets the identifier of the user performing impersonation, if applicable.
    /// </summary>
    string? ImpersonatingUserId { get; }
}
