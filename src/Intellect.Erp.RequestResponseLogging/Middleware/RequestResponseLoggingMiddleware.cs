using System.Diagnostics;
using System.Linq;
using System.Text;
using Intellect.Erp.RequestResponseLogging.Exceptions;
using Intellect.Erp.RequestResponseLogging.Helpers;
using Intellect.Erp.RequestResponseLogging.Models;
using Intellect.Erp.RequestResponseLogging.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Intellect.Erp.RequestResponseLogging.Middleware
{
    /// <summary>
    /// Middleware that captures structured request and response information for QA-style environments.
    /// </summary>
    public sealed class RequestResponseLoggingMiddleware
    {
        private static int _productionWarningEmitted;
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly RequestResponseLoggingOptions _options;
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestResponseLoggingMiddleware"/> class.
        /// </summary>
        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            IOptions<RequestResponseLoggingOptions> options,
            IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!_options.Enabled)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            var environmentName = _environment.EnvironmentName ?? string.Empty;

            if (EnvironmentValidator.IsDisallowedEnvironment(environmentName))
            {
                if (Interlocked.Exchange(ref _productionWarningEmitted, 1) == 0)
                {
                    _logger.LogWarning(
                        "RequestResponseLogging is enabled but disabled for production-like environment '{EnvironmentName}'. Payload capture is skipped.",
                        environmentName);
                }

                await _next(context).ConfigureAwait(false);
                return;
            }

            if (!EnvironmentValidator.IsAllowedEnvironment(environmentName, _options.AllowedEnvironments))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            if (RequestFilteringHelper.ShouldSkipPath(context.Request.Path, _options))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            var requestMetadata = await CaptureRequestMetadataAsync(context).ConfigureAwait(false);
            var originalResponseBody = context.Response.Body;
            MemoryStream? responseBuffer = null;

            if (_options.EnableResponseLogging)
            {
                responseBuffer = new MemoryStream();
                context.Response.Body = responseBuffer;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context).ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();

                ResponseMetadata responseMetadata;
                if (responseBuffer is not null)
                {
                    responseMetadata = await CaptureResponseMetadataAsync(context, responseBuffer).ConfigureAwait(false);
                    responseBuffer.Seek(0, SeekOrigin.Begin);
                    await responseBuffer.CopyToAsync(originalResponseBody).ConfigureAwait(false);
                    context.Response.Body = originalResponseBody;
                }
                else
                {
                    responseMetadata = new ResponseMetadata
                    {
                        StatusCode = context.Response.StatusCode,
                        ContentType = context.Response.ContentType ?? string.Empty,
                        Headers = _options.EnableHeaderLogging
                            ? PayloadMaskingHelper.MaskHeaders(context.Response.Headers, _options)
                            : new Dictionary<string, string>()
                    };
                }

                LogRequestResponse(context, requestMetadata, responseMetadata, environmentName, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task<RequestMetadata> CaptureRequestMetadataAsync(HttpContext context)
        {
            var request = context.Request;
            var requestMetadata = new RequestMetadata
            {
                Method = request.Method,
                Path = request.Path.Value ?? string.Empty,
                QueryString = PayloadMaskingHelper.MaskQueryString(request.QueryString.Value, _options.SensitiveFields),
                ContentType = request.ContentType ?? string.Empty,
                Headers = _options.EnableHeaderLogging
                    ? PayloadMaskingHelper.MaskHeaders(request.Headers, _options)
                    : new Dictionary<string, string>()
            };

            if (!_options.EnableRequestLogging || !RequestFilteringHelper.CanCaptureRequestBody(request))
            {
                return requestMetadata;
            }

            if (request.ContentLength.HasValue && request.ContentLength.Value > _options.MaxPayloadSizeBytes)
            {
                requestMetadata.PayloadTooLarge = true;
                requestMetadata.PayloadSizeBytes = request.ContentLength.GetValueOrDefault();
                requestMetadata.Payload = "[REQUEST PAYLOAD TOO LARGE]";
                return requestMetadata;
            }

            if (!RequestFilteringHelper.IsPayloadContentTypeAllowed(request.ContentType, _options))
            {
                requestMetadata.PayloadSizeBytes = request.ContentLength.GetValueOrDefault();
                requestMetadata.Payload = "[REQUEST PAYLOAD OMITTED]";
                return requestMetadata;
            }

            StreamHelper.EnsureRequestBuffering(request);
            request.Body.Seek(0, SeekOrigin.Begin);

            try
            {
                if (RequestFilteringHelper.IsMultipartFormData(request.ContentType))
                {
                    var form = await request.ReadFormAsync().ConfigureAwait(false);
                    requestMetadata.UploadedFiles = form.Files
                        .Select(file => file.FileName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToArray();

                    var maskedForm = JsonConvert.SerializeObject(PayloadMaskingHelper.MaskFormFields(form, _options.SensitiveFields), Formatting.None);
                    requestMetadata.PayloadSizeBytes = request.ContentLength ?? Encoding.UTF8.GetByteCount(maskedForm);
                    requestMetadata.Payload = PayloadFormattingHelper.TruncatePayload(maskedForm, _options);
                    requestMetadata.PayloadTruncated = maskedForm.Length > _options.MaxPayloadLogLength;
                }
                else if (RequestFilteringHelper.IsFormUrlEncoded(request.ContentType))
                {
                    var form = await request.ReadFormAsync().ConfigureAwait(false);
                    var maskedForm = JsonConvert.SerializeObject(PayloadMaskingHelper.MaskFormFields(form, _options.SensitiveFields), Formatting.None);
                    requestMetadata.PayloadSizeBytes = request.ContentLength ?? Encoding.UTF8.GetByteCount(maskedForm);
                    requestMetadata.Payload = PayloadFormattingHelper.TruncatePayload(maskedForm, _options);
                    requestMetadata.PayloadTruncated = maskedForm.Length > _options.MaxPayloadLogLength;
                }
                else
                {
                    var payload = await StreamHelper.ReadStreamAsStringAsync(request.Body, _options.MaxPayloadSizeBytes).ConfigureAwait(false);
                    requestMetadata.PayloadSizeBytes = Encoding.UTF8.GetByteCount(payload);

                    var maskedPayload = RequestFilteringHelper.IsJsonContentType(request.ContentType)
                        ? PayloadMaskingHelper.MaskJson(payload, _options.SensitiveFields)
                        : payload;

                    requestMetadata.Payload = PayloadFormattingHelper.TruncatePayload(maskedPayload, _options);
                    requestMetadata.PayloadTruncated = maskedPayload.Length > _options.MaxPayloadLogLength;
                }
            }
            catch (RequestBodyTooLargeException)
            {
                requestMetadata.PayloadTooLarge = true;
                requestMetadata.PayloadSizeBytes = request.ContentLength.GetValueOrDefault();
                requestMetadata.Payload = "[REQUEST PAYLOAD TOO LARGE]";
            }
            catch (Exception ex)
            {
                requestMetadata.PayloadSizeBytes = request.ContentLength.GetValueOrDefault();
                _logger.LogWarning(ex, "Failed to capture request payload for {Path}", request.Path);
                requestMetadata.Payload = "[REQUEST PAYLOAD FAILED TO CAPTURE]";
            }
            finally
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            return requestMetadata;
        }

        private async Task<ResponseMetadata> CaptureResponseMetadataAsync(HttpContext context, MemoryStream responseBodyBuffer)
        {
            responseBodyBuffer.Seek(0, SeekOrigin.Begin);
            var responseMetadata = new ResponseMetadata
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType ?? string.Empty,
                Headers = _options.EnableHeaderLogging
                    ? PayloadMaskingHelper.MaskHeaders(context.Response.Headers, _options)
                    : new Dictionary<string, string>()
            };

            responseMetadata.PayloadSizeBytes = responseBodyBuffer.Length;

            if (responseBodyBuffer.Length == 0)
            {
                responseMetadata.PayloadSizeCategory = PayloadClassificationHelper.Classify(responseMetadata.PayloadSizeBytes, _options.LargePayloadThresholdBytes);
                return responseMetadata;
            }

            if (responseBodyBuffer.Length > _options.MaxPayloadSizeBytes)
            {
                responseMetadata.PayloadTooLarge = true;
                responseMetadata.Payload = "[RESPONSE PAYLOAD TOO LARGE]";
                responseMetadata.PayloadSizeCategory = PayloadClassificationHelper.Classify(responseMetadata.PayloadSizeBytes, _options.LargePayloadThresholdBytes);
                return responseMetadata;
            }

            if (!RequestFilteringHelper.IsPayloadContentTypeAllowed(context.Response.ContentType, _options))
            {
                responseMetadata.Payload = "[RESPONSE PAYLOAD OMITTED]";
                responseMetadata.PayloadSizeCategory = PayloadClassificationHelper.Classify(responseMetadata.PayloadSizeBytes, _options.LargePayloadThresholdBytes);
                return responseMetadata;
            }

            try
            {
                var payload = await StreamHelper.ReadStreamAsStringAsync(responseBodyBuffer, _options.MaxPayloadSizeBytes).ConfigureAwait(false);
                responseMetadata.Payload = RequestFilteringHelper.IsJsonContentType(context.Response.ContentType)
                    ? PayloadMaskingHelper.MaskJson(payload, _options.SensitiveFields)
                    : PayloadFormattingHelper.TruncatePayload(payload, _options);

                responseMetadata.PayloadTruncated = responseMetadata.Payload.Length > _options.MaxPayloadLogLength;
            }
            catch (RequestBodyTooLargeException)
            {
                responseMetadata.PayloadTooLarge = true;
                responseMetadata.Payload = "[RESPONSE PAYLOAD TOO LARGE]";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture response payload for {Path}", context.Request.Path);
                responseMetadata.Payload = "[RESPONSE PAYLOAD FAILED TO CAPTURE]";
            }

            return responseMetadata;
        }

        private void LogRequestResponse(HttpContext context, RequestMetadata requestMetadata, ResponseMetadata responseMetadata, string environmentName, long elapsedMilliseconds)
        {
            requestMetadata.PayloadSizeCategory = PayloadClassificationHelper.Classify(requestMetadata.PayloadSizeBytes, _options.LargePayloadThresholdBytes);
            responseMetadata.PayloadSizeCategory = PayloadClassificationHelper.Classify(responseMetadata.PayloadSizeBytes, _options.LargePayloadThresholdBytes);

            var totalPayloadBytes = requestMetadata.PayloadSizeBytes + responseMetadata.PayloadSizeBytes;
            var performanceCategory = PerformanceClassificationHelper.Classify(elapsedMilliseconds, _options.SlowRequestThresholdMs);
            var isPerformanceIssue = PerformanceClassificationHelper.IsPerformanceIssue(elapsedMilliseconds, _options.SlowRequestThresholdMs);

            var logModel = new RequestResponseLogModel
            {
                Request = requestMetadata,
                Response = responseMetadata,
                CorrelationId = GetCorrelationId(context),
                ElapsedMilliseconds = elapsedMilliseconds,
                EnvironmentName = environmentName,
                MachineName = Environment.MachineName,
                Timestamp = DateTimeOffset.UtcNow,
                IsSlowRequest = elapsedMilliseconds >= _options.SlowRequestThresholdMs,
                TotalPayloadBytes = totalPayloadBytes,
                PerformanceCategory = performanceCategory,
                PayloadSizeCategory = PayloadClassificationHelper.Classify(totalPayloadBytes, _options.LargePayloadThresholdBytes),
                IsPayloadHeavyRequest = totalPayloadBytes >= _options.LargePayloadThresholdBytes,
                IsPerformanceIssue = isPerformanceIssue
            };

            if (logModel.IsSlowRequest)
            {
                _logger.LogWarning("SLOW REQUEST {@RequestResponse}", logModel);
                return;
            }

            _logger.LogInformation("HTTP Transaction {@RequestResponse}", logModel);
        }

        private static string GetCorrelationId(HttpContext context)
        {
            if (context.Items.TryGetValue("CorrelationId", out var itemValue) && itemValue is string itemCorrelationId && !string.IsNullOrWhiteSpace(itemCorrelationId))
            {
                return itemCorrelationId;
            }

            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerValues) && headerValues.Count > 0)
            {
                return headerValues[0]!;
            }

            return context.TraceIdentifier;
        }
    }
}
