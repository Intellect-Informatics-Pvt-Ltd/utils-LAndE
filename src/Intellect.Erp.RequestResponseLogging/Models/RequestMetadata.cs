namespace Intellect.Erp.RequestResponseLogging.Models
{
    /// <summary>
    /// Represents captured metadata for a request.
    /// </summary>
    public sealed class RequestMetadata
    {
        /// <summary>
        /// The HTTP method.
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// The request path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The masked query string.
        /// </summary>
        public string QueryString { get; set; } = string.Empty;

        /// <summary>
        /// The captured request payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Content type of the request.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Header values captured when header logging is enabled.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Uploaded file names captured for multipart requests.
        /// </summary>
        public IReadOnlyCollection<string> UploadedFiles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Indicates that the payload was truncated.
        /// </summary>
        public bool PayloadTruncated { get; set; }

        /// <summary>
        /// Indicates that the payload was too large to capture.
        /// </summary>
        public bool PayloadTooLarge { get; set; }

        /// <summary>
        /// The original payload size in bytes before masking or truncation.
        /// </summary>
        public long PayloadSizeBytes { get; set; }

        /// <summary>
        /// Categorized payload size for analytics and dashboards.
        /// </summary>
        public string PayloadSizeCategory { get; set; } = string.Empty;
    }
}
