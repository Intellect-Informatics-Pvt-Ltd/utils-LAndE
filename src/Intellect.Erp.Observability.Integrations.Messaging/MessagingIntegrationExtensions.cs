using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Intellect.Erp.Observability.Integrations.Messaging;

/// <summary>
/// Extension methods for registering Messaging integration services.
/// </summary>
public static class MessagingIntegrationExtensions
{
    /// <summary>
    /// Registers the <see cref="ObservabilityProducerContextEnricher"/> as the
    /// <see cref="IProducerContextEnricher"/> implementation, enriching Kafka event
    /// envelopes with correlation, user, and tenant context from the Observability platform.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessagingIntegration(this IServiceCollection services)
    {
        services.TryAddScoped<IProducerContextEnricher, ObservabilityProducerContextEnricher>();
        return services;
    }
}
