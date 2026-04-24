using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Abstractions;

public class RecordEqualityTests
{
    [Fact]
    public void FieldError_ValueEquality_EqualInstances()
    {
        var a = new FieldError("Name", "REQUIRED", "Name is required");
        var b = new FieldError("Name", "REQUIRED", "Name is required");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void FieldError_ValueEquality_DifferentInstances()
    {
        var a = new FieldError("Name", "REQUIRED", "Name is required");
        var b = new FieldError("Email", "REQUIRED", "Email is required");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ErrorCatalogEntry_ValueEquality_EqualInstances()
    {
        var a = new ErrorCatalogEntry(
            "ERP-CORE-SYS-0001", "System Error", "An error occurred", "Check logs",
            500, ErrorSeverity.Error, false, ErrorCategory.System);
        var b = new ErrorCatalogEntry(
            "ERP-CORE-SYS-0001", "System Error", "An error occurred", "Check logs",
            500, ErrorSeverity.Error, false, ErrorCategory.System);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void ErrorCatalogEntry_ValueEquality_DifferentInstances()
    {
        var a = new ErrorCatalogEntry(
            "ERP-CORE-SYS-0001", "System Error", "An error occurred", "Check logs",
            500, ErrorSeverity.Error, false, ErrorCategory.System);
        var b = new ErrorCatalogEntry(
            "ERP-CORE-VAL-0001", "Validation Error", "Invalid input", "Validate fields",
            400, ErrorSeverity.Warning, false, ErrorCategory.Validation);

        a.Should().NotBe(b);
    }

    [Fact]
    public void AuditEvent_ValueEquality_EqualInstances()
    {
        var data = new Dictionary<string, object?> { ["key"] = "value" };
        var timestamp = DateTimeOffset.UtcNow;

        var a = new AuditEvent(
            "evt-1", "corr-1", "Loans", "Disbursement", "Create",
            "user1", "tenant1", "pacs1", "Loan", "loan-123",
            AuditOutcome.Success, null, data, timestamp);
        var b = new AuditEvent(
            "evt-1", "corr-1", "Loans", "Disbursement", "Create",
            "user1", "tenant1", "pacs1", "Loan", "loan-123",
            AuditOutcome.Success, null, data, timestamp);

        a.Should().Be(b);
    }

    [Fact]
    public void AuditEvent_ValueEquality_DifferentOutcome()
    {
        var data = new Dictionary<string, object?>();
        var timestamp = DateTimeOffset.UtcNow;

        var a = new AuditEvent(
            "evt-1", "corr-1", "Loans", "Disbursement", "Create",
            "user1", "tenant1", "pacs1", "Loan", "loan-123",
            AuditOutcome.Success, null, data, timestamp);
        var b = new AuditEvent(
            "evt-1", "corr-1", "Loans", "Disbursement", "Create",
            "user1", "tenant1", "pacs1", "Loan", "loan-123",
            AuditOutcome.Failure, "ERP-CORE-SYS-0001", data, timestamp);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ErrorResponse_ValueEquality_EqualInstances()
    {
        var a = new ErrorResponse
        {
            Success = false,
            ErrorCode = "ERP-CORE-SYS-0001",
            Title = "System Error",
            Message = "An unexpected error occurred",
            CorrelationId = "corr-1",
            Status = 500,
            Severity = "Error",
            Retryable = false,
            Timestamp = "2024-01-01T00:00:00Z"
        };
        var b = new ErrorResponse
        {
            Success = false,
            ErrorCode = "ERP-CORE-SYS-0001",
            Title = "System Error",
            Message = "An unexpected error occurred",
            CorrelationId = "corr-1",
            Status = 500,
            Severity = "Error",
            Retryable = false,
            Timestamp = "2024-01-01T00:00:00Z"
        };

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void ErrorResponse_ValueEquality_DifferentInstances()
    {
        var a = new ErrorResponse
        {
            Success = false,
            ErrorCode = "ERP-CORE-SYS-0001",
            Title = "System Error",
            Message = "An unexpected error occurred",
            CorrelationId = "corr-1",
            Status = 500,
            Severity = "Error",
            Retryable = false,
            Timestamp = "2024-01-01T00:00:00Z"
        };
        var b = new ErrorResponse
        {
            Success = false,
            ErrorCode = "ERP-CORE-VAL-0001",
            Title = "Validation Error",
            Message = "Invalid input",
            CorrelationId = "corr-2",
            Status = 400,
            Severity = "Warning",
            Retryable = false,
            Timestamp = "2024-01-01T00:00:00Z"
        };

        a.Should().NotBe(b);
    }
}
