using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.AuditHooks;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.AuditHooks;

/// <summary>
/// Unit tests for <see cref="TraceabilityBridgeAuditHook"/> verifying it correctly maps
/// AuditEvent fields to AuditActivityRecord and routes through ITraceSink.
/// </summary>
public sealed class TraceabilityBridgeAuditHookTests
{
    [Fact]
    public async Task EmitAsync_RoutesRecordToTraceSink()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        sink.RecordedActivities.Should().ContainSingle();
    }

    [Fact]
    public async Task EmitAsync_MapsEventIdToActivityId()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        sink.RecordedActivities[0].ActivityId.Should().Be(auditEvent.EventId);
    }

    [Fact]
    public async Task EmitAsync_MapsCorrelationId()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        sink.RecordedActivities[0].CorrelationId.Should().Be(auditEvent.CorrelationId);
    }

    [Fact]
    public async Task EmitAsync_MapsModuleFeatureOperation()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var record = sink.RecordedActivities[0];
        record.Module.Should().Be("Loans");
        record.Feature.Should().Be("Disbursement");
        record.Operation.Should().Be("Create");
    }

    [Fact]
    public async Task EmitAsync_MapsActorAndTenantFields()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var record = sink.RecordedActivities[0];
        record.Actor.Should().Be("user@example.com");
        record.TenantId.Should().Be("tenant-1");
        record.PacsId.Should().Be("pacs-1");
    }

    [Fact]
    public async Task EmitAsync_MapsEntityTypeAndEntityId()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var record = sink.RecordedActivities[0];
        record.EntityType.Should().Be("Loan");
        record.EntityId.Should().Be("loan-123");
    }

    [Fact]
    public async Task EmitAsync_MapsOutcomeAsString()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        sink.RecordedActivities[0].Outcome.Should().Be("Success");
    }

    [Fact]
    public async Task EmitAsync_MapsErrorCode()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent() with { ErrorCode = "ERP-LOANS-BIZ-0001" };

        await hook.EmitAsync(auditEvent);

        sink.RecordedActivities[0].ErrorCode.Should().Be("ERP-LOANS-BIZ-0001");
    }

    [Fact]
    public async Task EmitAsync_MapsDataDictionary()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var record = sink.RecordedActivities[0];
        record.Data.Should().ContainKey("amount");
        record.Data["amount"].Should().Be(50000);
    }

    [Fact]
    public async Task EmitAsync_MapsOccurredAt()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var now = DateTimeOffset.UtcNow;
        var auditEvent = CreateSampleAuditEvent() with { OccurredAt = now };

        await hook.EmitAsync(auditEvent);

        sink.RecordedActivities[0].OccurredAt.Should().Be(now);
    }

    [Fact]
    public async Task EmitAsync_ThrowsOnNullAuditEvent()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);

        var act = () => hook.EmitAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullTraceSink()
    {
        var act = () => new TraceabilityBridgeAuditHook(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task EmitAsync_CreatesIndependentDataCopy()
    {
        var sink = new FakeTraceSink();
        var hook = new TraceabilityBridgeAuditHook(sink);
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        // Modifying the original data should not affect the recorded activity
        auditEvent.Data["newKey"] = "newValue";
        sink.RecordedActivities[0].Data.Should().NotContainKey("newKey");
    }

    private static AuditEvent CreateSampleAuditEvent() => new(
        EventId: "evt-001",
        CorrelationId: "corr-001",
        Module: "Loans",
        Feature: "Disbursement",
        Operation: "Create",
        Actor: "user@example.com",
        TenantId: "tenant-1",
        PacsId: "pacs-1",
        EntityType: "Loan",
        EntityId: "loan-123",
        Outcome: AuditOutcome.Success,
        ErrorCode: null,
        Data: new Dictionary<string, object?> { ["amount"] = 50000 },
        OccurredAt: DateTimeOffset.UtcNow);

    private sealed class FakeTraceSink : ITraceSink
    {
        public List<AuditActivityRecord> RecordedActivities { get; } = new();

        public Task RecordAsync(AuditActivityRecord record, CancellationToken cancellationToken = default)
        {
            RecordedActivities.Add(record);
            return Task.CompletedTask;
        }
    }
}
