using System.ComponentModel.DataAnnotations;

namespace Intellect.Erp.RequestResponseLogging.Options
{
    /// <summary>
    /// Configuration options for request/response logging middleware.
    /// </summary>
    public sealed class RequestResponseLoggingOptions
    {
        /// <summary>
        /// Enables or disables the middleware entirely.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Enables request payload capture and logging.
        /// </summary>
        public bool EnableRequestLogging { get; set; } = true;

        /// <summary>
        /// Enables response payload capture and logging.
        /// </summary>
        public bool EnableResponseLogging { get; set; } = true;

        /// <summary>
        /// Enables logging of selected headers.
        /// </summary>
        public bool EnableHeaderLogging { get; set; } = false;

        /// <summary>
        /// Enables logging for swagger endpoints when explicitly configured.
        /// </summary>
        public bool EnableSwaggerLogging { get; set; } = false;

        /// <summary>
        /// Enables logging for health check endpoints when explicitly configured.
        /// </summary>
        public bool EnableHealthCheckLogging { get; set; } = false;

        /// <summary>
        /// If true, long payload strings are truncated instead of logging full content.
        /// </summary>
        public bool TruncateLongPayloads { get; set; } = true;

        /// <summary>
        /// Maximum supported payload size in bytes before payload capture is skipped.
        /// </summary>
        public long MaxPayloadSizeBytes { get; set; } = 5 * 1024 * 1024;

        /// <summary>
        /// Maximum text length logged for payload values after optional truncation.
        /// </summary>
        public int MaxPayloadLogLength { get; set; } = 10_000;

        /// <summary>
        /// Threshold in milliseconds after which requests are logged as slow.
        /// </summary>
        public int SlowRequestThresholdMs { get; set; } = 3_000;

        /// <summary>
        /// Environments in which logging is permitted.
        /// </summary>
        public string[] AllowedEnvironments { get; set; } = new[]
        {
            "Development",
            "QA",
            "QualityAssurance",
            "UAT",
            "Staging"
        };

        /// <summary>
        /// Sensitive fields that must be masked when found in payloads.
        /// </summary>
        public string[] SensitiveFields { get; set; } = new[]
        {
            "password",
            "token",
            "authorization",
            "jwt",
            "secret",
            "apiKey",
            "aadhaar",
            "pan",
            "accessToken",
            "refreshToken"
        };

        /// <summary>
        /// Request paths that should never be logged.
        /// </summary>
        public string[] ExcludedPaths { get; set; } = new[]
        {
            "/swagger",
            "/health",
            "/metrics",
            "/favicon.ico",
            ".css",
            ".js",
            ".png",
            ".jpg",
            ".jpeg",
            ".svg",
            "/static",
            "/robots.txt"
        };

        /// <summary>
        /// Threshold in bytes above which payloads are classified as heavy.
        /// </summary>
        public long LargePayloadThresholdBytes { get; set; } = 1 * 1024 * 1024;

        /// <summary>
        /// Content types that are never logged.
        /// </summary>
        public string[] ExcludedContentTypes { get; set; } = new[]
        {
            "application/octet-stream",
            "image/",
            "video/",
            "audio/"
        };

        /// <summary>
        /// Headers included in logs when header logging is enabled.
        /// </summary>
        public string[] IncludedHeaders { get; set; } = new[]
        {
            "Content-Type",
            "User-Agent",
            "Host",
            "X-Correlation-ID"
        };

        /// <summary>
        /// Headers excluded from logs even when header logging is enabled.
        /// </summary>
        public string[] ExcludedHeaders { get; set; } = new[]
        {
            "Cookie",
            "Set-Cookie",
            "Authorization"
        };
    }
}
