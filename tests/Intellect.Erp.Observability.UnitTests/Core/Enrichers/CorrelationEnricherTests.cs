using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core.Enrichers;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core.Enrichers;

public class CorrelationEnricherTests
{
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new[] { new TextToken("Test") }),
            Enumerable.Empty<LogEventProperty>());
    }

    private static Serilog.Core.ILogEventPropertyFactory PropertyFactory()
        => new Serilog.Core.LoggingLevelSwitch() is not null
            ? new SimplePropertyFactory()
            : throw new InvalidOperationException();

    [Fact]
    public void Enrich_AddsCorrelationId_WhenAccessorReturnsValue()
    {
        var accessor = new FakeCorrelationAccessor { CorrelationId = "test-corr-123" };
        var enricher = new CorrelationEnricher(accessor);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().ContainKey("correlationId");
        evt.Properties["correlationId"].ToString().Should().Contain("test-corr-123");
    }

    [Fact]
    public void Enrich_DoesNotAddCorrelationId_WhenAccessorReturnsNull()
    {
        var accessor = new FakeCorrelationAccessor { CorrelationId = null };
        var enricher = new CorrelationEnricher(accessor);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().NotContainKey("correlationId");
    }

    [Fact]
    public void Enrich_SwallowsException_WhenAccessorThrows()
    {
        var accessor = new ThrowingCorrelationAccessor();
        var enricher = new CorrelationEnricher(accessor);
        var evt = CreateLogEvent();

        var act = () => enricher.Enrich(evt, new SimplePropertyFactory());

        act.Should().NotThrow();
    }

    [Fact]
    public void Enrich_IncrementsErrorCount_WhenAccessorThrows()
    {
        var accessor = new ThrowingCorrelationAccessor();
        var enricher = new CorrelationEnricher(accessor);
        var evt = CreateLogEvent();
        var before = CorrelationEnricher.ErrorCount;

        enricher.Enrich(evt, new SimplePropertyFactory());

        CorrelationEnricher.ErrorCount.Should().BeGreaterThan(before);
    }

    private class FakeCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? TraceParent { get; set; }
    }

    private class ThrowingCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId => throw new InvalidOperationException("boom");
        public string? CausationId => throw new InvalidOperationException("boom");
        public string? TraceParent => throw new InvalidOperationException("boom");
    }
}

public class UserContextEnricherTests
{
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new[] { new TextToken("Test") }),
            Enumerable.Empty<LogEventProperty>());
    }

    [Fact]
    public void Enrich_AddsUserIdUserNameRole_WhenAccessorReturnsValues()
    {
        var accessor = new FakeUserAccessor
        {
            UserId = "user-1",
            UserName = "John",
            Role = "Admin"
        };
        var redaction = new NoOpRedactionEngine();
        var enricher = new UserContextEnricher(accessor, redaction);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().ContainKey("userId");
        evt.Properties.Should().ContainKey("userName");
        evt.Properties.Should().ContainKey("role");
    }

    [Fact]
    public void Enrich_DoesNotAddFields_WhenAccessorReturnsNull()
    {
        var accessor = new FakeUserAccessor();
        var redaction = new NoOpRedactionEngine();
        var enricher = new UserContextEnricher(accessor, redaction);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().NotContainKey("userId");
        evt.Properties.Should().NotContainKey("userName");
        evt.Properties.Should().NotContainKey("role");
    }

    [Fact]
    public void Enrich_SwallowsException_WhenAccessorThrows()
    {
        var accessor = new ThrowingUserAccessor();
        var redaction = new NoOpRedactionEngine();
        var enricher = new UserContextEnricher(accessor, redaction);
        var evt = CreateLogEvent();

        var act = () => enricher.Enrich(evt, new SimplePropertyFactory());

        act.Should().NotThrow();
    }

    [Fact]
    public void Enrich_IncrementsErrorCount_WhenAccessorThrows()
    {
        var accessor = new ThrowingUserAccessor();
        var redaction = new NoOpRedactionEngine();
        var enricher = new UserContextEnricher(accessor, redaction);
        var evt = CreateLogEvent();
        var before = UserContextEnricher.ErrorCount;

        enricher.Enrich(evt, new SimplePropertyFactory());

        UserContextEnricher.ErrorCount.Should().BeGreaterThan(before);
    }

    private class FakeUserAccessor : IUserContextAccessor
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public string? ImpersonatingUserId { get; set; }
    }

    private class ThrowingUserAccessor : IUserContextAccessor
    {
        public string? UserId => throw new InvalidOperationException("boom");
        public string? UserName => throw new InvalidOperationException("boom");
        public string? Role => throw new InvalidOperationException("boom");
        public string? ImpersonatingUserId => throw new InvalidOperationException("boom");
    }
}

public class TenantContextEnricherTests
{
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new[] { new TextToken("Test") }),
            Enumerable.Empty<LogEventProperty>());
    }

    [Fact]
    public void Enrich_AddsTenantFields_WhenAccessorReturnsValues()
    {
        var accessor = new FakeTenantAccessor
        {
            TenantId = "T1",
            StateCode = "MH",
            PacsId = "P1",
            BranchCode = "BR01"
        };
        var enricher = new TenantContextEnricher(accessor);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().ContainKey("tenantId");
        evt.Properties.Should().ContainKey("stateCode");
        evt.Properties.Should().ContainKey("pacsId");
        evt.Properties.Should().ContainKey("branchCode");
    }

    [Fact]
    public void Enrich_DoesNotAddFields_WhenAccessorReturnsNull()
    {
        var accessor = new FakeTenantAccessor();
        var enricher = new TenantContextEnricher(accessor);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().NotContainKey("tenantId");
        evt.Properties.Should().NotContainKey("stateCode");
        evt.Properties.Should().NotContainKey("pacsId");
        evt.Properties.Should().NotContainKey("branchCode");
    }

    [Fact]
    public void Enrich_SwallowsException_WhenAccessorThrows()
    {
        var accessor = new ThrowingTenantAccessor();
        var enricher = new TenantContextEnricher(accessor);
        var evt = CreateLogEvent();

        var act = () => enricher.Enrich(evt, new SimplePropertyFactory());

        act.Should().NotThrow();
    }

    [Fact]
    public void Enrich_IncrementsErrorCount_WhenAccessorThrows()
    {
        var accessor = new ThrowingTenantAccessor();
        var enricher = new TenantContextEnricher(accessor);
        var evt = CreateLogEvent();
        var before = TenantContextEnricher.ErrorCount;

        enricher.Enrich(evt, new SimplePropertyFactory());

        TenantContextEnricher.ErrorCount.Should().BeGreaterThan(before);
    }

    private class FakeTenantAccessor : ITenantContextAccessor
    {
        public string? TenantId { get; set; }
        public string? StateCode { get; set; }
        public string? PacsId { get; set; }
        public string? BranchCode { get; set; }
    }

    private class ThrowingTenantAccessor : ITenantContextAccessor
    {
        public string? TenantId => throw new InvalidOperationException("boom");
        public string? StateCode => throw new InvalidOperationException("boom");
        public string? PacsId => throw new InvalidOperationException("boom");
        public string? BranchCode => throw new InvalidOperationException("boom");
    }
}

public class ModuleContextEnricherTests
{
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new[] { new TextToken("Test") }),
            Enumerable.Empty<LogEventProperty>());
    }

    [Fact]
    public void Enrich_AddsModuleFields_WhenAccessorReturnsValues()
    {
        var accessor = new FakeModuleAccessor
        {
            ModuleName = "Loans",
            ServiceName = "LoanService",
            Environment = "Development",
            Feature = "Disbursement",
            Operation = "Create"
        };
        var enricher = new ModuleContextEnricher(accessor);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().ContainKey("module");
        evt.Properties.Should().ContainKey("serviceName");
        evt.Properties.Should().ContainKey("env");
        evt.Properties.Should().ContainKey("feature");
        evt.Properties.Should().ContainKey("operation");
    }

    [Fact]
    public void Enrich_DoesNotAddFields_WhenAccessorReturnsNull()
    {
        var accessor = new FakeModuleAccessor();
        var enricher = new ModuleContextEnricher(accessor);
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().NotContainKey("module");
        evt.Properties.Should().NotContainKey("serviceName");
        evt.Properties.Should().NotContainKey("env");
    }

    [Fact]
    public void Enrich_SwallowsException_WhenAccessorThrows()
    {
        var accessor = new ThrowingModuleAccessor();
        var enricher = new ModuleContextEnricher(accessor);
        var evt = CreateLogEvent();

        var act = () => enricher.Enrich(evt, new SimplePropertyFactory());

        act.Should().NotThrow();
    }

    [Fact]
    public void Enrich_IncrementsErrorCount_WhenAccessorThrows()
    {
        var accessor = new ThrowingModuleAccessor();
        var enricher = new ModuleContextEnricher(accessor);
        var evt = CreateLogEvent();
        var before = ModuleContextEnricher.ErrorCount;

        enricher.Enrich(evt, new SimplePropertyFactory());

        ModuleContextEnricher.ErrorCount.Should().BeGreaterThan(before);
    }

    private class FakeModuleAccessor : IModuleContextAccessor
    {
        public string? ModuleName { get; set; }
        public string? ServiceName { get; set; }
        public string? Environment { get; set; }
        public string? Feature { get; set; }
        public string? Operation { get; set; }
    }

    private class ThrowingModuleAccessor : IModuleContextAccessor
    {
        public string? ModuleName => throw new InvalidOperationException("boom");
        public string? ServiceName => throw new InvalidOperationException("boom");
        public string? Environment => throw new InvalidOperationException("boom");
        public string? Feature => throw new InvalidOperationException("boom");
        public string? Operation => throw new InvalidOperationException("boom");
    }
}

public class MachineEnricherTests
{
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new[] { new TextToken("Test") }),
            Enumerable.Empty<LogEventProperty>());
    }

    [Fact]
    public void Enrich_AddsMachineProperty()
    {
        var enricher = new MachineEnricher();
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().ContainKey("machine");
        evt.Properties["machine"].ToString().Should().Contain(System.Environment.MachineName);
    }
}

public class SchemaVersionEnricherTests
{
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new[] { new TextToken("Test") }),
            Enumerable.Empty<LogEventProperty>());
    }

    [Fact]
    public void Enrich_AddsLogSchemaV1()
    {
        var enricher = new SchemaVersionEnricher();
        var evt = CreateLogEvent();

        enricher.Enrich(evt, new SimplePropertyFactory());

        evt.Properties.Should().ContainKey("log.schema");
        evt.Properties["log.schema"].ToString().Should().Contain("v1");
    }
}

/// <summary>
/// Simple property factory for test use.
/// </summary>
internal sealed class SimplePropertyFactory : Serilog.Core.ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        return new LogEventProperty(name, new ScalarValue(value));
    }
}

/// <summary>
/// No-op redaction engine for enricher tests.
/// </summary>
internal sealed class NoOpRedactionEngine : IRedactionEngine
{
    public string Redact(string value) => value;
    public System.Text.Json.JsonElement RedactJson(System.Text.Json.JsonElement element) => element;
    public IReadOnlyDictionary<string, object?> RedactProperties(IReadOnlyDictionary<string, object?> properties) => properties;
    public object RedactObject(object obj, Type? type = null) => obj;
}
