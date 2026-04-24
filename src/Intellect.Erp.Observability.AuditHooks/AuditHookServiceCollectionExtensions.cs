using Intellect.Erp.Observability.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Extension methods for registering audit hook services based on configuration.
/// </summary>
public static class AuditHookServiceCollectionExtensions
{
    /// <summary>
    /// Registers the appropriate <see cref="IAuditHook"/> implementation based on the
    /// <c>Observability:AuditHook:Mode</c> configuration value.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configured mode is unknown or when required dependencies are missing.
    /// </exception>
    public static IServiceCollection AddAuditHooks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(AuditHookOptions.SectionName);
        services.Configure<AuditHookOptions>(section);

        var options = new AuditHookOptions();
        section.Bind(options);

        switch (options.Mode)
        {
            case "LogOnly":
                services.TryAddSingleton<IAuditHook, LogOnlyAuditHook>();
                break;

            case "TraceabilityBridge":
                services.TryAddSingleton<IAuditHook, TraceabilityBridgeAuditHook>();
                break;

            case "Kafka":
                if (string.IsNullOrWhiteSpace(options.Topic))
                {
                    throw new InvalidOperationException(
                        "Observability:AuditHook:Topic must be configured when Mode is 'Kafka'.");
                }

                services.TryAddSingleton<IAuditHook>(sp =>
                {
                    var producer = sp.GetRequiredService<IKafkaProducer>();
                    return new KafkaAuditHook(producer, options.Topic);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown audit hook mode '{options.Mode}'. Valid values: LogOnly, TraceabilityBridge, Kafka.");
        }

        return services;
    }
}
