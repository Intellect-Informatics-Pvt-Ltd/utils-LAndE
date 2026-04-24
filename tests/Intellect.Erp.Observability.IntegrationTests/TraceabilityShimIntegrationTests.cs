using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Integrations.Traceability;
using Microsoft.Extensions.DependencyInjection;

namespace Intellect.Erp.Observability.IntegrationTests;

/// <summary>
/// Integration tests for the Traceability shim adapters verifying context delegation,
/// masking delegation, correlation passthrough, and audit adaptation.
/// </summary>
public class TraceabilityShimIntegrationTests
{
    #region Context Delegation Tests

    [Fact]
    public void TraceabilityCorrelationAdapter_DelegatesToTraceContext()
    {
        // Arrange
        var traceContext = new FakeTraceContextAccessor
        {
            CorrelationId = "corr-12345"
        };
        var adapter = new TraceabilityCorrelationAdapter(traceContext);

        // Act & Assert
        adapter.CorrelationId.Should().Be("corr-12345");
        adapter.CausationId.Should().BeNull("CausationId is not available from Traceability");
        adapter.TraceParent.Should().BeNull("TraceParent is not available from Traceability");
    }

    [Fact]
    public void TraceabilityUserAdapter_DelegatesToTraceContext()
    {
        // Arrange
        var traceContext = new FakeTraceContextAccessor
        {
            UserId = "user-001",
            UserName = "John Doe",
            Role = "Admin"
        };
        var adapter = new TraceabilityUserAdapter(traceContext);

        // Act & Assert
        adapter.UserId.Should().Be("user-001");
        adapter.UserName.Should().Be("John Doe");
        adapter.Role.Should().Be("Admin");
        adapter.ImpersonatingUserId.Should().BeNull("ImpersonatingUserId is not available from Traceability");
    }

    [Fact]
    public void TraceabilityTenantAdapter_DelegatesToTraceContext()
    {
        // Arrange
        var traceContext = new FakeTraceContextAccessor
        {
            TenantId = "tenant-100",
            StateCode = "KA",
            BranchCode = "BR-001"
        };
        var adapter = new TraceabilityTenantAdapter(traceContext);

        // Act & Assert
        adapter.TenantId.Should().Be("tenant-100");
        adapter.StateCode.Should().Be("KA");
        adapter.BranchCode.Should().Be("BR-001");
        adapter.PacsId.Should().BeNull("PacsId is not available from Traceability");
    }

    [Fact]
    public void TraceabilityAdapters_ReturnNull_WhenTraceContextValuesAreNull()
    {
        // Arrange
        var traceContext = new FakeTraceContextAccessor();
        var correlationAdapter = new TraceabilityCorrelationAdapter(traceContext);
        var userAdapter = new TraceabilityUserAdapter(traceContext);
        var tenantAdapter = new TraceabilityTenantAdapter(traceContext);

        // Act & Assert
        correlationAdapter.CorrelationId.Should().BeNull();
        userAdapter.UserId.Should().BeNull();
        userAdapter.UserName.Should().BeNull();
        userAdapter.Role.Should().BeNull();
        tenantAdapter.TenantId.Should().BeNull();
        tenantAdapter.StateCode.Should().BeNull();
        tenantAdapter.BranchCode.Should().BeNull();
    }

    #endregion

    #region Masking Delegation Tests

    [Fact]
    public void TraceabilityMaskingAdapter_DelegatesToMaskingPolicy()
    {
        // Arrange
        var policy = new FakeMaskingPolicy();
        var adapter = new TraceabilityMaskingAdapter(policy);

        // Act
        var result = adapter.Mask("$.body.password", "secret123");

        // Assert
        result.Should().Be("***secret123***",
            "the fake policy wraps the value with ***");
    }

    [Fact]
    public void TraceabilityMaskingAdapter_ReturnsNull_WhenPolicyReturnsNull()
    {
        // Arrange
        var policy = new NullReturningMaskingPolicy();
        var adapter = new TraceabilityMaskingAdapter(policy);

        // Act
        var result = adapter.Mask("$.body.field", "value");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TraceabilityMaskingAdapter_ThrowsOnNullPolicy()
    {
        // Act
        var act = () => new TraceabilityMaskingAdapter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("maskingPolicy");
    }

    #endregion

    #region Correlation Passthrough via DI Tests

    [Fact]
    public void AddTraceabilityIntegration_ReplacesAccessors_WhenTraceContextIsRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITraceContextAccessor>(_ => new FakeTraceContextAccessor
        {
            CorrelationId = "trace-corr-id",
            UserId = "trace-user",
            UserName = "Trace User",
            Role = "Operator",
            TenantId = "trace-tenant",
            StateCode = "MH",
            BranchCode = "BR-002"
        });

        // Act
        services.AddTraceabilityIntegration();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var correlationAccessor = scope.ServiceProvider.GetRequiredService<ICorrelationContextAccessor>();
        var userAccessor = scope.ServiceProvider.GetRequiredService<IUserContextAccessor>();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();

        // Assert
        correlationAccessor.Should().BeOfType<TraceabilityCorrelationAdapter>();
        correlationAccessor.CorrelationId.Should().Be("trace-corr-id");

        userAccessor.Should().BeOfType<TraceabilityUserAdapter>();
        userAccessor.UserId.Should().Be("trace-user");
        userAccessor.UserName.Should().Be("Trace User");
        userAccessor.Role.Should().Be("Operator");

        tenantAccessor.Should().BeOfType<TraceabilityTenantAdapter>();
        tenantAccessor.TenantId.Should().Be("trace-tenant");
        tenantAccessor.StateCode.Should().Be("MH");
        tenantAccessor.BranchCode.Should().Be("BR-002");
    }

    [Fact]
    public void AddTraceabilityIntegration_FallsBackToNullAccessors_WhenTraceContextNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act — no ITraceContextAccessor registered
        services.AddTraceabilityIntegration();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var correlationAccessor = scope.ServiceProvider.GetRequiredService<ICorrelationContextAccessor>();
        var userAccessor = scope.ServiceProvider.GetRequiredService<IUserContextAccessor>();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();

        // Assert — all return null but don't throw
        correlationAccessor.CorrelationId.Should().BeNull();
        userAccessor.UserId.Should().BeNull();
        tenantAccessor.TenantId.Should().BeNull();
    }

    [Fact]
    public void AddTraceabilityIntegration_RegistersMaskingAdapter_WhenPolicyIsAvailable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMaskingPolicy>(_ => new FakeMaskingPolicy());

        // Act
        services.AddTraceabilityIntegration();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var adapter = scope.ServiceProvider.GetService<TraceabilityMaskingAdapter>();

        // Assert
        adapter.Should().NotBeNull();
        adapter!.Mask("$.path", "value").Should().Be("***value***");
    }

    #endregion

    #region Audit Adaptation Tests

    [Fact]
    public void TraceabilityAuditAdapter_MapsAllFields()
    {
        // Arrange
        var occurredAt = DateTimeOffset.UtcNow;
        var auditEvent = new AuditEvent(
            EventId: "evt-001",
            CorrelationId: "corr-001",
            Module: "Loans",
            Feature: "Disbursement",
            Operation: "Create",
            Actor: "user-001",
            TenantId: "tenant-001",
            PacsId: "pacs-001",
            EntityType: "Loan",
            EntityId: "loan-123",
            Outcome: AuditOutcome.Success,
            ErrorCode: null,
            Data: new Dictionary<string, object?> { ["amount"] = 50000 },
            OccurredAt: occurredAt);

        // Act
        var record = TraceabilityAuditAdapter.ToActivityRecord(auditEvent);

        // Assert
        record.ActivityId.Should().Be("evt-001");
        record.CorrelationId.Should().Be("corr-001");
        record.Module.Should().Be("Loans");
        record.Feature.Should().Be("Disbursement");
        record.Operation.Should().Be("Create");
        record.Actor.Should().Be("user-001");
        record.TenantId.Should().Be("tenant-001");
        record.PacsId.Should().Be("pacs-001");
        record.EntityType.Should().Be("Loan");
        record.EntityId.Should().Be("loan-123");
        record.Outcome.Should().Be("Success");
        record.ErrorCode.Should().BeNull();
        record.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void TraceabilityAuditAdapter_MapsFailureOutcome()
    {
        // Arrange
        var auditEvent = new AuditEvent(
            EventId: "evt-002",
            CorrelationId: "corr-002",
            Module: "FAS",
            Feature: "Posting",
            Operation: "Approve",
            Actor: "user-002",
            TenantId: "tenant-002",
            PacsId: "pacs-002",
            EntityType: "Voucher",
            EntityId: "voucher-456",
            Outcome: AuditOutcome.Failure,
            ErrorCode: "ERP-FAS-BIZ-0001",
            Data: new Dictionary<string, object?>(),
            OccurredAt: DateTimeOffset.UtcNow);

        // Act
        var record = TraceabilityAuditAdapter.ToActivityRecord(auditEvent);

        // Assert
        record.Outcome.Should().Be("Failure");
        record.ErrorCode.Should().Be("ERP-FAS-BIZ-0001");
    }

    [Fact]
    public void TraceabilityAuditAdapter_MapsRejectedOutcome()
    {
        // Arrange
        var auditEvent = new AuditEvent(
            EventId: "evt-003",
            CorrelationId: "corr-003",
            Module: "Savings",
            Feature: "Withdrawal",
            Operation: "Process",
            Actor: "user-003",
            TenantId: "tenant-003",
            PacsId: "pacs-003",
            EntityType: "Account",
            EntityId: "acct-789",
            Outcome: AuditOutcome.Rejected,
            ErrorCode: "ERP-SAVINGS-BIZ-0002",
            Data: new Dictionary<string, object?>(),
            OccurredAt: DateTimeOffset.UtcNow);

        // Act
        var record = TraceabilityAuditAdapter.ToActivityRecord(auditEvent);

        // Assert
        record.Outcome.Should().Be("Rejected");
    }

    [Fact]
    public void TraceabilityAuditAdapter_ThrowsOnNullEvent()
    {
        // Act
        var act = () => TraceabilityAuditAdapter.ToActivityRecord(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Test Doubles

    private sealed class FakeTraceContextAccessor : ITraceContextAccessor
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public string? TenantId { get; set; }
        public string? StateCode { get; set; }
        public string? CorrelationId { get; set; }
        public string? BranchCode { get; set; }
    }

    private sealed class FakeMaskingPolicy : IMaskingPolicy
    {
        public string? Mask(string path, string? value) => $"***{value}***";
    }

    private sealed class NullReturningMaskingPolicy : IMaskingPolicy
    {
        public string? Mask(string path, string? value) => null;
    }

    #endregion
}
