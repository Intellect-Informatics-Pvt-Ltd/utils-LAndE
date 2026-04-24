using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Options;

namespace Intellect.Erp.Observability.AspNetCore.Accessors;

/// <summary>
/// <see cref="IModuleContextAccessor"/> backed by <see cref="ObservabilityOptions"/>.
/// Provides static module-level context (module name, application name, environment).
/// Feature and Operation are populated by the <see cref="Filters.BusinessOperationFilter"/>.
/// </summary>
public sealed class ConfigurationModuleContextAccessor : IModuleContextAccessor
{
    private readonly IOptions<ObservabilityOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationModuleContextAccessor"/>.
    /// </summary>
    public ConfigurationModuleContextAccessor(IOptions<ObservabilityOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string? ModuleName => _options.Value.ModuleName;

    /// <inheritdoc />
    public string? ServiceName => _options.Value.ApplicationName;

    /// <inheritdoc />
    public string? Environment => _options.Value.Environment;

    /// <inheritdoc />
    /// <remarks>
    /// Feature is set dynamically by the <see cref="Filters.BusinessOperationFilter"/>
    /// via LogContext. This accessor returns null; the enricher reads from LogContext.
    /// </remarks>
    public string? Feature => null;

    /// <inheritdoc />
    /// <remarks>
    /// Operation is set dynamically by the <see cref="Filters.BusinessOperationFilter"/>
    /// via LogContext. This accessor returns null; the enricher reads from LogContext.
    /// </remarks>
    public string? Operation => null;
}
