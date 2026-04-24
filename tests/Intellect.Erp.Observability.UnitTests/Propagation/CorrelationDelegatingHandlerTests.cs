using System.Diagnostics;
using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Propagation;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Propagation;

/// <summary>
/// Unit tests for <see cref="CorrelationDelegatingHandler"/> verifying header propagation
/// and W3C traceparent format.
/// </summary>
public class CorrelationDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_SetsCorrelationIdHeader_FromAccessor()
    {
        // Arrange
        var accessor = new FakeCorrelationAccessor { CorrelationId = "test-correlation-123" };
        var (invoker, captured) = CreateInvoker(accessor);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.GetValues("X-Correlation-Id").Should().ContainSingle()
            .Which.Should().Be("test-correlation-123");
    }

    [Fact]
    public async Task SendAsync_DoesNotSetCorrelationIdHeader_WhenNull()
    {
        // Arrange
        var accessor = new FakeCorrelationAccessor { CorrelationId = null };
        var (invoker, captured) = CreateInvoker(accessor);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.Contains("X-Correlation-Id").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_SetsTraceparentHeader_InW3CFormat_WhenActivityExists()
    {
        // Arrange
        var accessor = new FakeCorrelationAccessor { CorrelationId = "corr-1" };
        var (invoker, captured) = CreateInvoker(accessor);

        using var activitySource = new ActivitySource("test-traceparent");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("test-op");
        activity.Should().NotBeNull();

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.Contains("traceparent").Should().BeTrue();
        var traceparent = captured.Request.Headers.GetValues("traceparent").Single();
        traceparent.Should().MatchRegex(@"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$");
    }

    [Fact]
    public async Task SendAsync_DoesNotSetTraceparentHeader_WhenNoActivity()
    {
        // Arrange — ensure no Activity.Current
        Activity.Current = null;
        var accessor = new FakeCorrelationAccessor { CorrelationId = "corr-1" };
        var (invoker, captured) = CreateInvoker(accessor);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.Contains("traceparent").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_SetsCausationIdHeader_WhenAvailable()
    {
        // Arrange
        var accessor = new FakeCorrelationAccessor
        {
            CorrelationId = "corr-1",
            CausationId = "cause-abc"
        };
        var (invoker, captured) = CreateInvoker(accessor);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.GetValues("X-Causation-Id").Should().ContainSingle()
            .Which.Should().Be("cause-abc");
    }

    [Fact]
    public async Task SendAsync_DoesNotSetCausationIdHeader_WhenNull()
    {
        // Arrange
        var accessor = new FakeCorrelationAccessor { CorrelationId = "corr-1", CausationId = null };
        var (invoker, captured) = CreateInvoker(accessor);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.Contains("X-Causation-Id").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_SetsTenantAndStateHeaders_WhenAvailable()
    {
        // Arrange
        var correlationAccessor = new FakeCorrelationAccessor { CorrelationId = "corr-1" };
        var tenantAccessor = new FakeTenantAccessor
        {
            TenantId = "tenant-xyz",
            StateCode = "KA"
        };
        var (invoker, captured) = CreateInvoker(correlationAccessor, tenantAccessor);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.GetValues("X-Tenant").Should().ContainSingle()
            .Which.Should().Be("tenant-xyz");
        captured.Request.Headers.GetValues("X-State-Code").Should().ContainSingle()
            .Which.Should().Be("KA");
    }

    [Fact]
    public async Task SendAsync_DoesNotSetTenantHeaders_WhenNoTenantAccessor()
    {
        // Arrange
        var accessor = new FakeCorrelationAccessor { CorrelationId = "corr-1" };
        var (invoker, captured) = CreateInvoker(accessor, tenantAccessor: null);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/api"), CancellationToken.None);

        // Assert
        captured.Request!.Headers.Contains("X-Tenant").Should().BeFalse();
        captured.Request.Headers.Contains("X-State-Code").Should().BeFalse();
    }

    #region Helpers

    private static (HttpMessageInvoker Invoker, RequestCapture Captured) CreateInvoker(
        ICorrelationContextAccessor correlationAccessor,
        ITenantContextAccessor? tenantAccessor = null)
    {
        var capture = new RequestCapture();
        var handler = new CorrelationDelegatingHandler(correlationAccessor, tenantAccessor)
        {
            InnerHandler = new CapturingHandler(capture)
        };
        return (new HttpMessageInvoker(handler), capture);
    }

    internal sealed class RequestCapture
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

    internal sealed class FakeCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? TraceParent { get; set; }
    }

    internal sealed class FakeTenantAccessor : ITenantContextAccessor
    {
        public string? TenantId { get; set; }
        public string? StateCode { get; set; }
        public string? PacsId { get; set; }
        public string? BranchCode { get; set; }
    }

    #endregion
}
