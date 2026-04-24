using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Intellect.Erp.Observability.Core;

/// <summary>
/// Configuration options for the observability platform.
/// Binds to the <c>Observability</c> configuration section.
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Observability";

    /// <summary>Gets or sets the application name.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the module name.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the deployment environment (e.g., "Development", "Production").</summary>
    public string Environment { get; set; } = "Development";

    /// <summary>Gets or sets the sink configuration.</summary>
    public SinkOptions Sinks { get; set; } = new();

    /// <summary>Gets or sets the masking configuration.</summary>
    public MaskingOptions Masking { get; set; } = new();

    /// <summary>Gets or sets the request logging configuration.</summary>
    public RequestLoggingOptions RequestLogging { get; set; } = new();

    /// <summary>Gets or sets per-namespace minimum log level overrides.</summary>
    public Dictionary<string, string> ModuleOverrides { get; set; } = new();

    /// <summary>Gets or sets the telemetry configuration.</summary>
    public TelemetryOptions Telemetry { get; set; } = new();
}

/// <summary>Sink configuration options.</summary>
public sealed class SinkOptions
{
    /// <summary>Gets or sets the console sink configuration.</summary>
    public ConsoleSinkOptions Console { get; set; } = new();

    /// <summary>Gets or sets the file sink configuration.</summary>
    public FileSinkOptions File { get; set; } = new();

    /// <summary>Gets or sets the Elasticsearch sink configuration.</summary>
    public ElasticsearchSinkOptions Elasticsearch { get; set; } = new();
}

/// <summary>Console sink configuration.</summary>
public sealed class ConsoleSinkOptions
{
    /// <summary>Gets or sets whether the console sink is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets whether to use compact JSON format. If false, uses template format.</summary>
    public bool CompactFormat { get; set; } = true;

    /// <summary>Gets or sets the output template when CompactFormat is false.</summary>
    public string OutputTemplate { get; set; } =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
}

/// <summary>File sink configuration.</summary>
public sealed class FileSinkOptions
{
    /// <summary>Gets or sets whether the file sink is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the log file path.</summary>
    public string Path { get; set; } = "logs/app-.log";

    /// <summary>Gets or sets the rolling interval.</summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>Gets or sets whether to use JSON format for file output.</summary>
    public bool JsonFormat { get; set; } = true;
}

/// <summary>Elasticsearch sink configuration.</summary>
public sealed class ElasticsearchSinkOptions
{
    /// <summary>Gets or sets whether the Elasticsearch sink is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the Elasticsearch node URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the index format pattern.</summary>
    public string IndexFormat { get; set; } = "{0:yyyy.MM}";

    /// <summary>Gets or sets the disk buffer path for offline buffering.</summary>
    public string BufferPath { get; set; } = "logs/es-buffer";
}

/// <summary>Masking configuration options.</summary>
public sealed class MaskingOptions
{
    /// <summary>Gets or sets whether masking is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the structural JSON path patterns to mask.</summary>
    public string[] Paths { get; set; } = [];

    /// <summary>Gets or sets additional regex patterns (format: "pattern|replacement").</summary>
    public string[] Regexes { get; set; } = [];

    /// <summary>Gets or sets whether to delegate to the Traceability masking policy.</summary>
    public bool UseTraceabilityPolicy { get; set; }
}

/// <summary>Request logging configuration options.</summary>
public sealed class RequestLoggingOptions
{
    /// <summary>Gets or sets whether to capture request bodies.</summary>
    public bool CaptureRequestBody { get; set; }

    /// <summary>Gets or sets whether to capture response bodies.</summary>
    public bool CaptureResponseBody { get; set; }

    /// <summary>Gets or sets the slow request threshold in milliseconds.</summary>
    public int SlowRequestThresholdMs { get; set; } = 3000;

    /// <summary>Gets or sets paths to exclude from request logging.</summary>
    public string[] ExcludePaths { get; set; } = ["/health", "/metrics", "/swagger"];

    /// <summary>Gets or sets the whitelist of routes for body capture.</summary>
    public string[] BodyWhitelist { get; set; } = [];
}

/// <summary>Telemetry configuration options.</summary>
public sealed class TelemetryOptions
{
    /// <summary>Gets or sets the health check endpoint path.</summary>
    public string HealthCheckPath { get; set; } = "/health/observability";
}

/// <summary>
/// Custom validator for <see cref="ObservabilityOptions"/> that enforces cross-field rules.
/// </summary>
public sealed class ObservabilityOptionsValidator : IValidateOptions<ObservabilityOptions>
{
    public ValidateOptionsResult Validate(string? name, ObservabilityOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            failures.Add("ApplicationName must be non-empty.");
        }

        if (string.IsNullOrWhiteSpace(options.ModuleName))
        {
            failures.Add("ModuleName must be non-empty.");
        }

        // ES URL must be parseable when ES sink is enabled
        if (options.Sinks.Elasticsearch.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Sinks.Elasticsearch.Url) ||
                !Uri.TryCreate(options.Sinks.Elasticsearch.Url, UriKind.Absolute, out _))
            {
                failures.Add("Elasticsearch URL must be a valid absolute URI when the Elasticsearch sink is enabled.");
            }
        }

        // Masking must be enabled in Production
        if (string.Equals(options.Environment, "Production", StringComparison.OrdinalIgnoreCase) &&
            !options.Masking.Enabled)
        {
            failures.Add("Masking must be enabled when Environment is 'Production'.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
