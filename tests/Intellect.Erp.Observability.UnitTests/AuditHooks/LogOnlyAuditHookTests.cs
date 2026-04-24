using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.AuditHooks;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.AuditHooks;

/// <summary>
/// Unit tests for <see cref="LogOnlyAuditHook"/> verifying it emits a structured log
/// at Information level with the audit.v1 tag.
/// </summary>
public sealed class LogOnlyAuditHookTests : IDisposable
{
    private readonly List<LogEvent> _capturedEvents = new();
    private readonly Serilog.Core.Logger _serilogLogger;
    private readonly LogOnlyAuditHook _hook;

    public LogOnlyAuditHookTests()
    {
        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new InMemoryTestSink(_capturedEvents))
            .CreateLogger();

        _hook = new LogOnlyAuditHook(_serilogLogger);
    }

    public void Dispose()
    {
        _serilogLogger.Dispose();
    }

    [Fact]
    public async Task EmitAsync_WritesLogAtInformationLevel()
    {
        var auditEvent = CreateSampleAuditEvent();

        await _hook.EmitAsync(auditEvent);

        _capturedEvents.Should().ContainSingle();
        _capturedEvents[0].Level.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public async Task EmitAsync_IncludesAuditV1Tag()
    {
        var auditEvent = CreateSampleAuditEvent();

        await _hook.EmitAsync(auditEvent);

        _capturedEvents.Should().ContainSingle();
        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("audit.v1");
        var value = evt.Properties["audit.v1"];
        value.Should().BeOfType<ScalarValue>()
            .Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task EmitAsync_IncludesEventIdInLog()
    {
        var auditEvent = CreateSampleAuditEvent();

        await _hook.EmitAsync(auditEvent);

        _capturedEvents.Should().ContainSingle();
        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("EventId");
        evt.Properties["EventId"].ToString().Should().Contain(auditEvent.EventId);
    }

    [Fact]
    public async Task EmitAsync_IncludesModuleFeatureOperationInLog()
    {
        var auditEvent = CreateSampleAuditEvent();

        await _hook.EmitAsync(auditEvent);

        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("Module");
        evt.Properties["Module"].ToString().Should().Contain("Loans");
        evt.Properties.Should().ContainKey("Feature");
        evt.Properties["Feature"].ToString().Should().Contain("Disbursement");
        evt.Properties.Should().ContainKey("Operation");
        evt.Properties["Operation"].ToString().Should().Contain("Create");
    }

    [Fact]
    public async Task EmitAsync_IncludesOutcomeInLog()
    {
        var auditEvent = CreateSampleAuditEvent();

        await _hook.EmitAsync(auditEvent);

        var evt = _capturedEvents[0];
        evt.Properties.Should().ContainKey("Outcome");
        evt.Properties["Outcome"].ToString().Should().Contain("Success");
    }

    [Fact]
    public async Task EmitAsync_ThrowsOnNullAuditEvent()
    {
        var act = () => _hook.EmitAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        var act = () => new LogOnlyAuditHook(null!);

        act.Should().Throw<ArgumentNullException>();
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

    private sealed class InMemoryTestSink : Serilog.Core.ILogEventSink
    {
        private readonly List<LogEvent> _events;

        public InMemoryTestSink(List<LogEvent> events)
        {
            _events = events;
        }

        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }
}
