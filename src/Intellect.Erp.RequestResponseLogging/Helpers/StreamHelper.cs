using System.Text;
using Intellect.Erp.RequestResponseLogging.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Intellect.Erp.RequestResponseLogging.Helpers
{
    /// <summary>
    /// Provides stream utilities for safe request and response body capture.
    /// </summary>
    internal static class StreamHelper
    {
        /// <summary>
        /// Ensures request buffering is enabled so the body can be read and rewound.
        /// </summary>
        public static void EnsureRequestBuffering(HttpRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }
        }

        /// <summary>
        /// Reads the stream as a UTF-8 string while enforcing a maximum byte limit.
        /// </summary>
        public static async Task<string> ReadStreamAsStringAsync(Stream body, long maxBytes)
        {
            if (body is null)
            {
                return string.Empty;
            }

            if (!body.CanSeek)
            {
                throw new InvalidOperationException("Stream must be seekable before reading.");
            }

            body.Seek(0, SeekOrigin.Begin);
            await using var memoryStream = new MemoryStream();
            var buffer = new byte[81920];
            int bytesRead;
            long totalBytes = 0;

            while ((bytesRead = await body.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false)) > 0)
            {
                totalBytes += bytesRead;

                if (totalBytes > maxBytes)
                {
                    throw new RequestBodyTooLargeException($"[PAYLOAD TOO LARGE: {maxBytes} bytes limit reached]");
                }

                memoryStream.Write(buffer, 0, bytesRead);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);
            body.Seek(0, SeekOrigin.Begin);
            return content;
        }
    }
}
