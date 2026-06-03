namespace Intellect.Erp.RequestResponseLogging.Models
{
    /// <summary>
    /// Represents a structured request/response log entry.
    /// </summary>
    public sealed class RequestResponseLogModel
    {
        /// <summary>
        /// Captured request metadata.
        /// </summary>
        public RequestMetadata Request { get; set; } = new RequestMetadata();

        /// <summary>
        /// Captured response metadata.
        /// </summary>
        public ResponseMetadata Response { get; set; } = new ResponseMetadata();

        /// <summary>
        /// Correlation identifier associated with the request.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Elapsed request processing time in milliseconds.
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// The current environment name.
        /// </summary>
        public string EnvironmentName { get; set; } = string.Empty;

        /// <summary>
        /// The machine name where the application is running.
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// The time the request was logged.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Indicates whether the request exceeded the slow request threshold.
        /// </summary>
        public bool IsSlowRequest { get; set; }

        /// <summary>
        /// Combined request and response payload size in bytes.
        /// </summary>
        public long TotalPayloadBytes { get; set; }

        /// <summary>
        /// Categorized performance classification for the request.
        /// </summary>
        public string PerformanceCategory { get; set; } = string.Empty;

        /// <summary>
        /// Summary payload classification for the request/response transaction.
        /// </summary>
        public string PayloadSizeCategory { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this request is payload-heavy.
        /// </summary>
        public bool IsPayloadHeavyRequest { get; set; }

        /// <summary>
        /// Indicates whether this request represents a performance issue.
        /// </summary>
        public bool IsPerformanceIssue { get; set; }
    }
}
