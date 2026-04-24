using Intellect.Erp.Observability.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Intellect.Erp.Observability.Integrations.Traceability;

/// <summary>
/// Extension methods for registering Traceability integration adapters.
/// </summary>
public static class TraceabilityIntegrationExtensions
{
    /// <summary>
    /// Replaces the default Observability context accessors with Traceability-backed adapters
    /// when <see cref="ITraceContextAccessor"/> is resolvable from the service provider.
    /// Also registers the <see cref="TraceabilityMaskingAdapter"/> when <see cref="IMaskingPolicy"/>
    /// is available.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTraceabilityIntegration(this IServiceCollection services)
    {
        // Replace correlation accessor with Traceability adapter
        services.RemoveAll<ICorrelationContextAccessor>();
        services.AddScoped<ICorrelationContextAccessor>(sp =>
        {
            var traceContext = sp.GetService<ITraceContextAccessor>();
            if (traceContext is not null)
            {
                return new TraceabilityCorrelationAdapter(traceContext);
            }

            // Fall back to any previously registered accessor or return a no-op
            return new NullCorrelationContextAccessor();
        });

        // Replace user accessor with Traceability adapter
        services.RemoveAll<IUserContextAccessor>();
        services.AddScoped<IUserContextAccessor>(sp =>
        {
            var traceContext = sp.GetService<ITraceContextAccessor>();
            if (traceContext is not null)
            {
                return new TraceabilityUserAdapter(traceContext);
            }

            return new NullUserContextAccessor();
        });

        // Replace tenant accessor with Traceability adapter
        services.RemoveAll<ITenantContextAccessor>();
        services.AddScoped<ITenantContextAccessor>(sp =>
        {
            var traceContext = sp.GetService<ITraceContextAccessor>();
            if (traceContext is not null)
            {
                return new TraceabilityTenantAdapter(traceContext);
            }

            return new NullTenantContextAccessor();
        });

        // Register masking adapter when IMaskingPolicy is available
        services.AddScoped(sp =>
        {
            var maskingPolicy = sp.GetService<IMaskingPolicy>();
            return maskingPolicy is not null
                ? new TraceabilityMaskingAdapter(maskingPolicy)
                : null!;
        });

        return services;
    }

    /// <summary>
    /// Fallback correlation accessor that returns null for all properties.
    /// </summary>
    private sealed class NullCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId => null;
        public string? CausationId => null;
        public string? TraceParent => null;
    }

    /// <summary>
    /// Fallback user accessor that returns null for all properties.
    /// </summary>
    private sealed class NullUserContextAccessor : IUserContextAccessor
    {
        public string? UserId => null;
        public string? UserName => null;
        public string? Role => null;
        public string? ImpersonatingUserId => null;
    }

    /// <summary>
    /// Fallback tenant accessor that returns null for all properties.
    /// </summary>
    private sealed class NullTenantContextAccessor : ITenantContextAccessor
    {
        public string? TenantId => null;
        public string? StateCode => null;
        public string? PacsId => null;
        public string? BranchCode => null;
    }
}
