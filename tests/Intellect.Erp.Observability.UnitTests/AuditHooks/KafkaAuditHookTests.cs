using System.Text.Json;
using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.AuditHooks;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.AuditHooks;

/// <summary>
/// Unit tests for <see cref="KafkaAuditHook"/> verifying it serializes AuditEvent to JSON
/// and publishes to the configured topic.
/// </summary>
public sealed class KafkaAuditHookTests
{
    [Fact]
    public async Task EmitAsync_PublishesToConfiguredTopic()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        producer.PublishedMessages.Should().ContainSingle();
        producer.PublishedMessages[0].Topic.Should().Be("audit-events");
    }

    [Fact]
    public async Task EmitAsync_UsesEventIdAsKey()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        producer.PublishedMessages[0].Key.Should().Be(auditEvent.EventId);
    }

    [Fact]
    public async Task EmitAsync_SerializesAuditEventToJson()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var json = producer.PublishedMessages[0].Value;
        json.Should().NotBeNullOrWhiteSpace();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("eventId").GetString().Should().Be("evt-001");
        root.GetProperty("correlationId").GetString().Should().Be("corr-001");
        root.GetProperty("module").GetString().Should().Be("Loans");
        root.GetProperty("feature").GetString().Should().Be("Disbursement");
        root.GetProperty("operation").GetString().Should().Be("Create");
        root.GetProperty("actor").GetString().Should().Be("user@example.com");
        root.GetProperty("tenantId").GetString().Should().Be("tenant-1");
        root.GetProperty("pacsId").GetString().Should().Be("pacs-1");
        root.GetProperty("entityType").GetString().Should().Be("Loan");
        root.GetProperty("entityId").GetString().Should().Be("loan-123");
    }

    [Fact]
    public async Task EmitAsync_SerializesOutcomeField()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var json = producer.PublishedMessages[0].Value;
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("outcome").GetInt32().Should().Be((int)AuditOutcome.Success);
    }

    [Fact]
    public async Task EmitAsync_OmitsNullErrorCode()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var json = producer.PublishedMessages[0].Value;
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("errorCode", out _).Should().BeFalse();
    }

    [Fact]
    public async Task EmitAsync_IncludesErrorCodeWhenPresent()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");
        var auditEvent = CreateSampleAuditEvent() with { ErrorCode = "ERP-LOANS-BIZ-0001" };

        await hook.EmitAsync(auditEvent);

        var json = producer.PublishedMessages[0].Value;
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("ERP-LOANS-BIZ-0001");
    }

    [Fact]
    public async Task EmitAsync_SerializesDataDictionary()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");
        var auditEvent = CreateSampleAuditEvent();

        await hook.EmitAsync(auditEvent);

        var json = producer.PublishedMessages[0].Value;
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("amount").GetInt32().Should().Be(50000);
    }

    [Fact]
    public async Task EmitAsync_ThrowsOnNullAuditEvent()
    {
        var producer = new FakeKafkaProducer();
        var hook = new KafkaAuditHook(producer, "audit-events");

        var act = () => hook.EmitAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullProducer()
    {
        var act = () => new KafkaAuditHook(null!, "topic");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyTopic()
    {
        var producer = new FakeKafkaProducer();

        var act = () => new KafkaAuditHook(producer, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnWhitespaceTopic()
    {
        var producer = new FakeKafkaProducer();

        var act = () => new KafkaAuditHook(producer, "   ");

        act.Should().Throw<ArgumentException>();
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

    private sealed class FakeKafkaProducer : IKafkaProducer
    {
        public List<(string Topic, string Key, string Value)> PublishedMessages { get; } = new();

        public Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add((topic, key, value));
            return Task.CompletedTask;
        }
    }
}
