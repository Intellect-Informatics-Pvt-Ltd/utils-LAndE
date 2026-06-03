using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Intellect.Erp.RequestResponseLogging.Options;

namespace Intellect.Erp.RequestResponseLogging.Extensions
{
    /// <summary>
    /// Service collection extension methods for request/response logging registration.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds request/response logging services and configuration binding from the provided configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddRequestResponseLogging(this IServiceCollection services, IConfiguration configuration)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddOptions<RequestResponseLoggingOptions>()
                .Bind(configuration.GetSection("RequestResponseLogging"))
                .ValidateDataAnnotations();

            return services;
        }

        /// <summary>
        /// Adds request/response logging services with inline options configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action used to configure the logging options.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddRequestResponseLogging(this IServiceCollection services, Action<RequestResponseLoggingOptions> configureOptions)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions is null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.AddOptions<RequestResponseLoggingOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations();

            return services;
        }

        /// <summary>
        /// Adds request/response logging services with configuration binding and inline overrides.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="configureOptions">Action used to override options after configuration binding.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddRequestResponseLogging(this IServiceCollection services, IConfiguration configuration, Action<RequestResponseLoggingOptions> configureOptions)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configureOptions is null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.AddOptions<RequestResponseLoggingOptions>()
                .Bind(configuration.GetSection("RequestResponseLogging"))
                .Configure(configureOptions)
                .ValidateDataAnnotations();

            return services;
        }
    }
}
