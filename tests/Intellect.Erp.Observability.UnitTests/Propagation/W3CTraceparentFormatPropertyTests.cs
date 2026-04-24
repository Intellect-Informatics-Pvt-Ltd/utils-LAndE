using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Propagation;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Propagation;

/// <summary>
/// Property-based test: W3C traceparent format.
/// For any Activity with trace/span IDs, the traceparent header matches the W3C regex.
/// **Validates: Requirements 1.6**
/// </summary>
public class W3CTraceparentFormatPropertyTests
{
    private static readonly Regex W3CTraceparentRegex = new(
        @"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Property 4: For any Activity with a trace ID and span ID, the traceparent header
    /// must match the W3C format: 00-{traceId32hex}-{spanId16hex}-{flags2hex}.
    /// **Validates: Requirements 1.6**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(HexIdArbitrary) }, MaxTest = 100)]
    public void Traceparent_Matches_W3C_Format_For_Any_Activity(HexIds ids)
    {
        // Arrange
        var accessor = new FakeCorrelationAccessor { CorrelationId = "test" };
        var capture = new RequestCapture();
        var handler = new CorrelationDelegatingHandler(accessor)
        {
            InnerHandler = new CapturingHandler(capture)
        };
        var invoker = new HttpMessageInvoker(handler);

        // Create an Activity with the generated trace/span IDs
        using var activitySource = new ActivitySource($"test-{Guid.NewGuid()}");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var traceId = ActivityTraceId.CreateFromString(ids.TraceId.AsSpan());
        var spanId = ActivitySpanId.CreateFromString(ids.SpanId.AsSpan());
        var context = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);

        using var activity = activitySource.StartActivity("test-op", ActivityKind.Client, context);
        activity.Should().NotBeNull("Activity should be created with the listener");

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
        invoker.SendAsync(request, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        capture.Request!.Headers.Contains("traceparent").Should().BeTrue();
        var traceparent = capture.Request.Headers.GetValues("traceparent").Single();
        W3CTraceparentRegex.IsMatch(traceparent).Should().BeTrue(
            $"traceparent '{traceparent}' should match W3C format");
    }

    #region Generators

    /// <summary>
    /// Holds generated hex trace and span IDs.
    /// </summary>
    public sealed class HexIds
    {
        public required string TraceId { get; init; }
        public required string SpanId { get; init; }
    }

    public static class HexIdArbitrary
    {
        public static Arbitrary<HexIds> HexIds()
        {
            var hexChar = Gen.Elements(
                '0', '1', '2', '3', '4', '5', '6', '7',
                '8', '9', 'a', 'b', 'c', 'd', 'e', 'f');

            // Generate 32-char hex for trace ID (must not be all zeros)
            var traceIdGen = from chars in Gen.ArrayOf(32, hexChar)
                             let s = new string(chars)
                             where s != "00000000000000000000000000000000"
                             select s;

            // Generate 16-char hex for span ID (must not be all zeros)
            var spanIdGen = from chars in Gen.ArrayOf(16, hexChar)
                            let s = new string(chars)
                            where s != "0000000000000000"
                            select s;

            var gen = from traceId in traceIdGen
                      from spanId in spanIdGen
                      select new HexIds { TraceId = traceId, SpanId = spanId };

            return Arb.From(gen);
        }
    }

    #endregion

    #region Helpers

    private sealed class RequestCapture
    {
        public HttpRequestMessage? Request { get; set; }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly RequestCapture _capture;

        public CapturingHandler(RequestCapture capture) => _capture = capture;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _capture.Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class FakeCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? TraceParent { get; set; }
    }

    #endregion
}
