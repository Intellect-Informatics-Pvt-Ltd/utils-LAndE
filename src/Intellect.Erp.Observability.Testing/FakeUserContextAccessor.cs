using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// Fake implementation of <see cref="IUserContextAccessor"/> with settable properties
/// for use in unit and integration tests.
/// </summary>
public sealed class FakeUserContextAccessor : IUserContextAccessor
{
    /// <inheritdoc />
    public string? UserId { get; set; }

    /// <inheritdoc />
    public string? UserName { get; set; }

    /// <inheritdoc />
    public string? Role { get; set; }

    /// <inheritdoc />
    public string? ImpersonatingUserId { get; set; }
}
