using Microsoft.AspNetCore.Http;
using Intellect.Erp.RequestResponseLogging.Options;

namespace Intellect.Erp.RequestResponseLogging.Helpers
{
    /// <summary>
    /// Filters requests and content types to protect log safety and reduce noise.
    /// </summary>
    internal static class RequestFilteringHelper
    {
        /// <summary>
        /// Determines whether the specified request path should be excluded from logging.
        /// </summary>
        public static bool ShouldSkipPath(PathString path, RequestResponseLoggingOptions options)
        {
            if (options is null)
            {
                return false;
            }

            var normalizedPath = path.Value?.Trim().ToLowerInvariant() ?? string.Empty;

            if (!options.EnableSwaggerLogging && normalizedPath.Contains("/swagger"))
            {
                return true;
            }

            if (!options.EnableHealthCheckLogging &&
                (normalizedPath.Contains("/health") || normalizedPath.Contains("/metrics")))
            {
                return true;
            }

            return options.ExcludedPaths
                .Any(excluded => !string.IsNullOrWhiteSpace(excluded) && normalizedPath.Contains(excluded.Trim().ToLowerInvariant()));
        }

        /// <summary>
        /// Determines whether the content type should not be logged.
        /// </summary>
        public static bool IsPayloadContentTypeAllowed(string? contentType, RequestResponseLoggingOptions options)
        {
            if (options is null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                return true;
            }

            var normalizedContentType = contentType.Trim().ToLowerInvariant();
            return !options.ExcludedContentTypes.Any(excluded => normalizedContentType.Contains(excluded.Trim().ToLowerInvariant()));
        }

        /// <summary>
        /// Determines whether the request payload should be captured.
        /// </summary>
        public static bool CanCaptureRequestBody(HttpRequest request)
        {
            return request.Body != null
                && !HttpMethods.IsGet(request.Method)
                && !HttpMethods.IsHead(request.Method)
                && request.ContentLength != 0;
        }

        /// <summary>
        /// Determines whether the content type is JSON.
        /// </summary>
        public static bool IsJsonContentType(string? contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                   && (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                       || contentType.Contains("application/*+json", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines whether the content type is multipart/form-data.
        /// </summary>
        public static bool IsMultipartFormData(string? contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                && contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the content type is form-urlencoded.
        /// </summary>
        public static bool IsFormUrlEncoded(string? contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                && contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the content type is text-based.
        /// </summary>
        public static bool IsTextBasedContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return true;
            }

            return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("application/*+json", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("application/xml", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("text/", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase);
        }
    }
}
