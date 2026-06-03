namespace Intellect.Erp.RequestResponseLogging.Models
{
    /// <summary>
    /// Represents captured metadata for a response.
    /// </summary>
    public sealed class ResponseMetadata
    {
        /// <summary>
        /// The response status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// The captured response payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Response content type.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Header values captured when header logging is enabled.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Indicates that the response payload was truncated.
        /// </summary>
        public bool PayloadTruncated { get; set; }

        /// <summary>
        /// Indicates that the response payload was too large to capture.
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
