using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using EsSinkOptions = Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions;

namespace Intellect.Erp.Observability.Core;

/// <summary>
/// Extension methods for <see cref="IHostBuilder"/> to configure Serilog-based observability.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog from <see cref="ObservabilityOptions"/> and registers
    /// Console, File, and Elasticsearch sinks based on configuration.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder UseObservability(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfig) =>
        {
            var options = new ObservabilityOptions();
            context.Configuration.GetSection(ObservabilityOptions.SectionName).Bind(options);

            loggerConfig
                .MinimumLevel.Debug()
                .Enrich.FromLogContext();

            // Apply module-level overrides
            foreach (var kvp in options.ModuleOverrides)
            {
                if (Enum.TryParse<LogEventLevel>(kvp.Value, ignoreCase: true, out var level))
                {
                    loggerConfig.MinimumLevel.Override(kvp.Key, level);
                }
            }

            // Console sink
            if (options.Sinks.Console.Enabled)
            {
                if (options.Sinks.Console.CompactFormat)
                {
                    loggerConfig.WriteTo.Console(new CompactJsonFormatter());
                }
                else
                {
                    loggerConfig.WriteTo.Console(outputTemplate: options.Sinks.Console.OutputTemplate);
                }
            }

            // File sink
            if (options.Sinks.File.Enabled)
            {
                var rollingInterval = Enum.TryParse<RollingInterval>(
                    options.Sinks.File.RollingInterval, ignoreCase: true, out var ri)
                    ? ri
                    : RollingInterval.Day;

                if (options.Sinks.File.JsonFormat)
                {
                    loggerConfig.WriteTo.File(
                        new CompactJsonFormatter(),
                        options.Sinks.File.Path,
                        rollingInterval: rollingInterval);
                }
                else
                {
                    loggerConfig.WriteTo.File(
                        options.Sinks.File.Path,
                        rollingInterval: rollingInterval);
                }
            }

            // Elasticsearch sink
            if (options.Sinks.Elasticsearch.Enabled &&
                Uri.TryCreate(options.Sinks.Elasticsearch.Url, UriKind.Absolute, out var esUri))
            {
                var appName = options.ApplicationName.ToLowerInvariant();
                var env = options.Environment.ToLowerInvariant();
                var indexFormat = $"{appName}-{env}-{options.Sinks.Elasticsearch.IndexFormat}";

                loggerConfig.WriteTo.Elasticsearch(new EsSinkOptions(esUri)
                {
                    AutoRegisterTemplate = true,
                    IndexFormat = indexFormat,
                    BufferBaseFilename = options.Sinks.Elasticsearch.BufferPath
                });
            }
        });
    }
}
