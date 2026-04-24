using Intellect.Erp.ErrorHandling;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.AspNetCore.Accessors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Intellect.Erp.Observability.AspNetCore;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register ASP.NET Core
/// observability and error handling services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the error handling subsystem: <see cref="IErrorFactory"/>,
    /// <see cref="IErrorCatalog"/> loaded from YAML files, and <see cref="ErrorHandlingOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddErrorHandling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind and validate ErrorHandlingOptions
        services.AddOptions<ErrorHandlingOptions>()
            .Bind(configuration.GetSection(ErrorHandlingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register IErrorFactory
        services.TryAddSingleton<IErrorFactory, DefaultErrorFactory>();

        // Register IErrorCatalog from YAML files
        services.TryAddSingleton<IErrorCatalog>(sp =>
        {
            var options = configuration.GetSection(ErrorHandlingOptions.SectionName)
                .Get<ErrorHandlingOptions>() ?? new ErrorHandlingOptions();

            var logger = sp.GetRequiredService<ILogger<InMemoryErrorCatalog>>();
            var allEntries = new List<ErrorCatalogEntry>();

            foreach (var catalogFile in options.CatalogFiles)
            {
                try
                {
                    var entries = YamlErrorCatalogLoader.Load(catalogFile);
                    allEntries.AddRange(entries);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to load error catalog file: {CatalogFile}", catalogFile);
                }
            }

            return new InMemoryErrorCatalog(allEntries);
        });

        return services;
    }

    /// <summary>
    /// Registers HttpContext-backed context accessors for the observability platform:
    /// <see cref="ICorrelationContextAccessor"/>, <see cref="IUserContextAccessor"/>,
    /// <see cref="ITenantContextAccessor"/>, and <see cref="IModuleContextAccessor"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObservabilityAccessors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        services.TryAddSingleton<ICorrelationContextAccessor, HttpContextCorrelationContextAccessor>();
        services.TryAddSingleton<IUserContextAccessor, HttpContextUserContextAccessor>();
        services.TryAddSingleton<ITenantContextAccessor, HttpContextTenantContextAccessor>();
        services.TryAddSingleton<IModuleContextAccessor, ConfigurationModuleContextAccessor>();

        return services;
    }
}
