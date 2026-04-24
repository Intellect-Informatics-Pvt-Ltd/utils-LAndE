using System.Diagnostics;
using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Integrations.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Integration tests for the Messaging shim verifying envelope enrichment
/// and W3C traceparent format.
/// </summary>
public class MessagingShimIntegrationTests
{
    #region Envelope Enrichment Tests

    [Fact]
    public void Enrich_SetsCorrelationId_FromAccessor()
    {
        // Arrange
        var enricher = CreateEnricher(correlationId: "msg-corr-001");
        var envelope = new EventEnvelope();

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.CorrelationId.Should().Be("msg-corr-001");
    }

    [Fact]
    public void Enrich_SetsCausationId_FromAccessor()
    {
        // Arrange
        var enricher = CreateEnricher(causationId: "cause-001");
        var envelope = new EventEnvelope();

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.CausationId.Should().Be("cause-001");
    }

    [Fact]
    public void Enrich_SetsUserId_FromAccessor()
    {
        // Arrange
        var enricher = CreateEnricher(userId: "user-msg-001");
        var envelope = new EventEnvelope();

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.UserId.Should().Be("user-msg-001");
    }

    [Fact]
    public void Enrich_SetsTenantId_FromAccessor()
    {
        // Arrange
        var enricher = CreateEnricher(tenantId: "tenant-msg-001");
        var envelope = new EventEnvelope();

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.TenantId.Should().Be("tenant-msg-001");
    }

    [Fact]
    public void Enrich_SetsAllFields_WhenAllContextAvailable()
    {
        // Arrange
        var enricher = CreateEnricher(
            correlationId: "corr-full",
            causationId: "cause-full",
            userId: "user-full",
            tenantId: "tenant-full");
        var envelope = new EventEnvelope();

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.CorrelationId.Should().Be("corr-full");
        envelope.CausationId.Should().Be("cause-full");
        envelope.UserId.Should().Be("user-full");
        envelope.TenantId.Should().Be("tenant-full");
    }

    [Fact]
    public void Enrich_DoesNotOverwriteExistingValues_WhenAccessorReturnsNull()
    {
        // Arrange
        var enricher = CreateEnricher(); // all nulls
        var envelope = new EventEnvelope
        {
            CorrelationId = "existing-corr",
            UserId = "existing-user",
            TenantId = "existing-tenant"
        };

        // Act
        enricher.Enrich(envelope);

        // Assert — null values should not overwrite existing
        envelope.CorrelationId.Should().Be("existing-corr");
        envelope.UserId.Should().Be("existing-user");
        envelope.TenantId.Should().Be("existing-tenant");
    }

    [Fact]
    public void Enrich_ThrowsOnNullEnvelope()
    {
        // Arrange
        var enricher = CreateEnricher();

        // Act
        var act = () => enricher.Enrich(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region W3C Traceparent Format Tests

    [Fact]
    public void Enrich_SetsTraceparent_InW3CFormat_WhenActivityExists()
    {
        // Arrange
        var enricher = CreateEnricher();
        var envelope = new EventEnvelope();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("test.messaging");
        using var activity = source.StartActivity("test-operation");

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.TraceParent.Should().NotBeNullOrEmpty();
        envelope.TraceParent.Should().MatchRegex(@"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$",
            "traceparent must follow W3C format: 00-{traceId32hex}-{spanId16hex}-{flags2hex}");
    }

    [Fact]
    public void Enrich_DoesNotSetTraceparent_WhenNoActivityExists()
    {
        // Arrange — ensure no Activity.Current
        var previousActivity = Activity.Current;
        Activity.Current = null;

        try
        {
            var enricher = CreateEnricher();
            var envelope = new EventEnvelope();

            // Act
            enricher.Enrich(envelope);

            // Assert
            envelope.TraceParent.Should().BeNull();
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [Fact]
    public void Enrich_TraceparentContainsCorrectTraceAndSpanIds()
    {
        // Arrange
        var enricher = CreateEnricher();
        var envelope = new EventEnvelope();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("test.messaging.ids");
        using var activity = source.StartActivity("test-op");

        var expectedTraceId = activity!.TraceId.ToHexString();
        var expectedSpanId = activity.SpanId.ToHexString();

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.TraceParent.Should().StartWith($"00-{expectedTraceId}-{expectedSpanId}-");
    }

    #endregion

    #region DI Registration Tests

    [Fact]
    public void AddMessagingIntegration_RegistersEnricher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICorrelationContextAccessor>(_ => new FakeCorrelationAccessor());
        services.AddScoped<IUserContextAccessor>(_ => new FakeUserAccessor());
        services.AddScoped<ITenantContextAccessor>(_ => new FakeTenantAccessor());

        // Act
        services.AddMessagingIntegration();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var enricher = scope.ServiceProvider.GetService<IProducerContextEnricher>();

        // Assert
        enricher.Should().NotBeNull();
        enricher.Should().BeOfType<ObservabilityProducerContextEnricher>();
    }

    [Fact]
    public void AddMessagingIntegration_EnricherWorksEndToEnd()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICorrelationContextAccessor>(_ => new FakeCorrelationAccessor
        {
            CorrelationId = "e2e-corr",
            CausationId = "e2e-cause"
        });
        services.AddScoped<IUserContextAccessor>(_ => new FakeUserAccessor { UserId = "e2e-user" });
        services.AddScoped<ITenantContextAccessor>(_ => new FakeTenantAccessor { TenantId = "e2e-tenant" });

        services.AddMessagingIntegration();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var enricher = scope.ServiceProvider.GetRequiredService<IProducerContextEnricher>();
        var envelope = new EventEnvelope();

        // Act
        enricher.Enrich(envelope);

        // Assert
        envelope.CorrelationId.Should().Be("e2e-corr");
        envelope.CausationId.Should().Be("e2e-cause");
        envelope.UserId.Should().Be("e2e-user");
        envelope.TenantId.Should().Be("e2e-tenant");
    }

    #endregion

    #region Helpers

    private static ObservabilityProducerContextEnricher CreateEnricher(
        string? correlationId = null,
        string? causationId = null,
        string? userId = null,
        string? tenantId = null)
    {
        return new ObservabilityProducerContextEnricher(
            new FakeCorrelationAccessor { CorrelationId = correlationId, CausationId = causationId },
            new FakeUserAccessor { UserId = userId },
            new FakeTenantAccessor { TenantId = tenantId });
    }

    #endregion

    #region Test Doubles

    private sealed class FakeCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? TraceParent { get; set; }
    }

    private sealed class FakeUserAccessor : IUserContextAccessor
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public string? ImpersonatingUserId { get; set; }
    }

    private sealed class FakeTenantAccessor : ITenantContextAccessor
    {
        public string? TenantId { get; set; }
        public string? StateCode { get; set; }
        public string? PacsId { get; set; }
        public string? BranchCode { get; set; }
    }

    #endregion
}
