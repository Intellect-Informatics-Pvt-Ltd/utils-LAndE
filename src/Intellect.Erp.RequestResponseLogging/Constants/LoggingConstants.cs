namespace Intellect.Erp.RequestResponseLogging.Constants
{
    /// <summary>
    /// Constants used by the request/response logging middleware.
    /// </summary>
    internal static class LoggingConstants
    {
        public static readonly string[] DefaultAllowedEnvironments =
        {
            "Development",
            "QA",
            "QualityAssurance",
            "UAT",
            "Staging"
        };

        public static readonly string[] DefaultExcludedPaths =
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

        public static readonly string[] DefaultExcludedContentTypes =
        {
            "application/octet-stream",
            "image/",
            "video/",
            "audio/"
        };

        public static readonly string[] DefaultSensitiveFields =
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

        public static readonly string[] DefaultIncludedHeaders =
        {
            "Content-Type",
            "User-Agent",
            "Host",
            "X-Correlation-ID"
        };

        public static readonly string[] DefaultExcludedHeaders =
        {
            "Cookie",
            "Set-Cookie",
            "Authorization"
        };
    }
}
