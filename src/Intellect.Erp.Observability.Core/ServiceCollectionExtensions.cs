using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core.Enrichers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Serilog.Core;

namespace Intellect.Erp.Observability.Core;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register observability services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers observability services: <see cref="IAppLogger{T}"/>, <see cref="IRedactionEngine"/>,
    /// all Serilog enrichers, and <see cref="ObservabilityOptions"/> with validation.
    /// Does NOT register context accessors (those come from the AspNetCore package).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate ObservabilityOptions
        services.AddOptions<ObservabilityOptions>()
            .Bind(configuration.GetSection(ObservabilityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IValidateOptions<ObservabilityOptions>, ObservabilityOptionsValidator>();

        // Register IAppLogger<T> as open generic
        services.TryAdd(ServiceDescriptor.Singleton(typeof(IAppLogger<>), typeof(AppLogger<>)));

        // Register IRedactionEngine as singleton
        services.TryAddSingleton<IRedactionEngine, DefaultRedactionEngine>();

        // Register all enrichers as singletons
        services.TryAddSingleton<CorrelationEnricher>();
        services.TryAddSingleton<UserContextEnricher>();
        services.TryAddSingleton<TenantContextEnricher>();
        services.TryAddSingleton<ModuleContextEnricher>();
        services.TryAddSingleton<MachineEnricher>();
        services.TryAddSingleton<SchemaVersionEnricher>();

        // Also register enrichers as ILogEventEnricher for Serilog pipeline discovery
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILogEventEnricher, CorrelationEnricher>(sp => sp.GetRequiredService<CorrelationEnricher>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILogEventEnricher, UserContextEnricher>(sp => sp.GetRequiredService<UserContextEnricher>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILogEventEnricher, TenantContextEnricher>(sp => sp.GetRequiredService<TenantContextEnricher>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILogEventEnricher, ModuleContextEnricher>(sp => sp.GetRequiredService<ModuleContextEnricher>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILogEventEnricher, MachineEnricher>(sp => sp.GetRequiredService<MachineEnricher>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILogEventEnricher, SchemaVersionEnricher>(sp => sp.GetRequiredService<SchemaVersionEnricher>()));

        return services;
    }
}
