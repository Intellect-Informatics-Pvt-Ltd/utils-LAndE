namespace Intellect.Erp.RequestResponseLogging.Exceptions
{
    /// <summary>
    /// Thrown when a request payload exceeds the configured maximum payload size.
    /// </summary>
    public sealed class RequestBodyTooLargeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBodyTooLargeException"/> class.
        /// </summary>
        /// <param name="message">The detailed error message.</param>
        public RequestBodyTooLargeException(string message)
            : base(message)
        {
        }
    }
}
