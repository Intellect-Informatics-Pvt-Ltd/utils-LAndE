using System.Text.Json;
using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Abstractions;

public class SerializationTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void ErrorResponse_SerializesToCamelCaseJson()
    {
        var response = new ErrorResponse
        {
            Success = false,
            ErrorCode = "ERP-CORE-SYS-0001",
            Title = "System Error",
            Message = "An unexpected error occurred",
            CorrelationId = "01HXYZ1234567890ABCDEFGHIJ",
            Status = 500,
            Severity = "Error",
            Retryable = false,
            Timestamp = "2024-01-15T10:30:00Z"
        };

        var json = JsonSerializer.Serialize(response, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("errorCode").GetString().Should().Be("ERP-CORE-SYS-0001");
        root.GetProperty("title").GetString().Should().Be("System Error");
        root.GetProperty("message").GetString().Should().Be("An unexpected error occurred");
        root.GetProperty("correlationId").GetString().Should().Be("01HXYZ1234567890ABCDEFGHIJ");
        root.GetProperty("status").GetInt32().Should().Be(500);
        root.GetProperty("severity").GetString().Should().Be("Error");
        root.GetProperty("retryable").GetBoolean().Should().BeFalse();
        root.GetProperty("timestamp").GetString().Should().Be("2024-01-15T10:30:00Z");
    }

    [Fact]
    public void ErrorResponse_WithFieldErrors_SerializesCorrectly()
    {
        var response = new ErrorResponse
        {
            Success = false,
            ErrorCode = "ERP-CORE-VAL-0001",
            Title = "Validation Error",
            Message = "One or more fields are invalid",
            CorrelationId = "corr-1",
            Status = 400,
            Severity = "Warning",
            Retryable = false,
            Timestamp = "2024-01-15T10:30:00Z",
            FieldErrors = new[]
            {
                new FieldError("Name", "REQUIRED", "Name is required"),
                new FieldError("Email", "FORMAT", "Email format is invalid")
            }
        };

        var json = JsonSerializer.Serialize(response, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var fieldErrors = root.GetProperty("fieldErrors");
        fieldErrors.GetArrayLength().Should().Be(2);

        var first = fieldErrors[0];
        first.GetProperty("field").GetString().Should().Be("Name");
        first.GetProperty("code").GetString().Should().Be("REQUIRED");
        first.GetProperty("message").GetString().Should().Be("Name is required");
    }

    [Fact]
    public void FieldError_SerializesToCamelCaseJson()
    {
        var fieldError = new FieldError("AccountNumber", "MAX_LENGTH", "Account number exceeds maximum length");

        var json = JsonSerializer.Serialize(fieldError, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("field").GetString().Should().Be("AccountNumber");
        root.GetProperty("code").GetString().Should().Be("MAX_LENGTH");
        root.GetProperty("message").GetString().Should().Be("Account number exceeds maximum length");
    }

    [Fact]
    public void ErrorCatalogEntry_SerializesToCamelCaseJson()
    {
        var entry = new ErrorCatalogEntry(
            "ERP-CORE-SYS-0001",
            "System Error",
            "An unexpected error occurred",
            "Check application logs for stack trace",
            500,
            ErrorSeverity.Error,
            false,
            ErrorCategory.System);

        var json = JsonSerializer.Serialize(entry, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("code").GetString().Should().Be("ERP-CORE-SYS-0001");
        root.GetProperty("title").GetString().Should().Be("System Error");
        root.GetProperty("userMessage").GetString().Should().Be("An unexpected error occurred");
        root.GetProperty("supportMessage").GetString().Should().Be("Check application logs for stack trace");
        root.GetProperty("httpStatus").GetInt32().Should().Be(500);
        root.GetProperty("severity").GetInt32().Should().Be((int)ErrorSeverity.Error);
        root.GetProperty("retryable").GetBoolean().Should().BeFalse();
        root.GetProperty("category").GetInt32().Should().Be((int)ErrorCategory.System);
    }

    [Fact]
    public void ErrorResponse_NullOptionalFields_AreOmittedOrNull()
    {
        var response = new ErrorResponse
        {
            Success = false,
            ErrorCode = "ERP-CORE-SYS-0001",
            Title = "System Error",
            Message = "Error",
            CorrelationId = "corr-1",
            Status = 500,
            Severity = "Error",
            Retryable = false,
            Timestamp = "2024-01-15T10:30:00Z",
            TraceId = null,
            FieldErrors = null,
            ExceptionType = null,
            StackTrace = null,
            SupportReference = null,
            Type = null
        };

        var json = JsonSerializer.Serialize(response, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Nullable fields should serialize as null (System.Text.Json default)
        root.GetProperty("traceId").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("fieldErrors").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("exceptionType").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("type").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
