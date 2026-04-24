using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Propagation;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Propagation;

/// <summary>
/// Property-based test: Outbound HTTP correlation propagation.
/// For any correlation ID in scope, the outbound request header matches.
/// **Validates: Requirements 1.5**
/// </summary>
public class OutboundCorrelationPropertyTests
{
    /// <summary>
    /// Property 3: For any non-empty correlation ID set on the accessor,
    /// the outbound HTTP request X-Correlation-Id header must carry the exact same value.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(NonEmptyStringArbitrary) }, MaxTest = 100)]
    public void Outbound_CorrelationId_Header_Matches_Accessor_Value(NonEmptyString correlationId)
    {
        // Arrange
        var id = correlationId.Get;
        var accessor = new FakeCorrelationAccessor { CorrelationId = id };
        var capture = new RequestCapture();
        var handler = new CorrelationDelegatingHandler(accessor)
        {
            InnerHandler = new CapturingHandler(capture)
        };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");

        // Act
        invoker.SendAsync(request, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        capture.Request!.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        capture.Request.Headers.GetValues("X-Correlation-Id").Should().ContainSingle()
            .Which.Should().Be(id);
    }

    #region Helpers

    public static class NonEmptyStringArbitrary
    {
        public static Arbitrary<NonEmptyString> NonEmptyString()
        {
            return Arb.Default.NonEmptyString();
        }
    }

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
