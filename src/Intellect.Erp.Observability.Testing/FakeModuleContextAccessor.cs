using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Testing;

/// <summary>
/// Fake implementation of <see cref="IModuleContextAccessor"/> with settable properties
/// for use in unit and integration tests.
/// </summary>
public sealed class FakeModuleContextAccessor : IModuleContextAccessor
{
    /// <inheritdoc />
    public string? ModuleName { get; set; }

    /// <inheritdoc />
    public string? ServiceName { get; set; }

    /// <inheritdoc />
    public string? Environment { get; set; }

    /// <inheritdoc />
    public string? Feature { get; set; }

    /// <inheritdoc />
    public string? Operation { get; set; }
}
