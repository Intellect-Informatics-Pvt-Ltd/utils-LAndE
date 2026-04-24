using System.Text;
using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Propagation;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Propagation;

/// <summary>
/// Unit tests for <see cref="KafkaHeaders"/> verifying write/read round-trip and null handling.
/// </summary>
public class KafkaHeadersTests
{
    [Fact]
    public void WriteAndRead_RoundTrip_PreservesAllValues()
    {
        // Arrange
        var headers = new Dictionary<string, byte[]>();
        var correlationAccessor = new FakeCorrelationAccessor
        {
            CorrelationId = "corr-123",
            CausationId = "cause-456",
            TraceParent = "00-abcdef1234567890abcdef1234567890-1234567890abcdef-01"
        };
        var tenantAccessor = new FakeTenantAccessor { TenantId = "tenant-xyz" };

        // Act
        KafkaHeaders.WriteCorrelation(headers, correlationAccessor, tenantAccessor, userId: "user-789");
        var result = KafkaHeaders.ReadCorrelation(headers);

        // Assert
        result.CorrelationId.Should().Be("corr-123");
        result.CausationId.Should().Be("cause-456");
        result.TraceParent.Should().Be("00-abcdef1234567890abcdef1234567890-1234567890abcdef-01");
        result.TenantId.Should().Be("tenant-xyz");
        result.UserId.Should().Be("user-789");
    }

    [Fact]
    public void WriteCorrelation_WithNullValues_DoesNotWriteNullHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, byte[]>();
        var correlationAccessor = new FakeCorrelationAccessor
        {
            CorrelationId = "corr-only",
            CausationId = null,
            TraceParent = null
        };

        // Act
        KafkaHeaders.WriteCorrelation(headers, correlationAccessor, tenantAccessor: null, userId: null);

        // Assert
        headers.Should().ContainKey("correlationId");
        headers.Should().NotContainKey("causationId");
        headers.Should().NotContainKey("traceparent");
        headers.Should().NotContainKey("tenantId");
        headers.Should().NotContainKey("userId");
    }

    [Fact]
    public void ReadCorrelation_WithMissingHeaders_ReturnsNulls()
    {
        // Arrange
        var headers = new Dictionary<string, byte[]>();

        // Act
        var result = KafkaHeaders.ReadCorrelation(headers);

        // Assert
        result.CorrelationId.Should().BeNull();
        result.CausationId.Should().BeNull();
        result.TraceParent.Should().BeNull();
        result.TenantId.Should().BeNull();
        result.UserId.Should().BeNull();
    }

    [Fact]
    public void WriteCorrelation_WithAllNullAccessorValues_WritesNothing()
    {
        // Arrange
        var headers = new Dictionary<string, byte[]>();
        var correlationAccessor = new FakeCorrelationAccessor();

        // Act
        KafkaHeaders.WriteCorrelation(headers, correlationAccessor);

        // Assert
        headers.Should().BeEmpty();
    }

    [Fact]
    public void WriteCorrelation_ValuesAreUtf8Encoded()
    {
        // Arrange
        var headers = new Dictionary<string, byte[]>();
        var correlationAccessor = new FakeCorrelationAccessor { CorrelationId = "test-utf8" };

        // Act
        KafkaHeaders.WriteCorrelation(headers, correlationAccessor);

        // Assert
        headers.Should().ContainKey("correlationId");
        Encoding.UTF8.GetString(headers["correlationId"]).Should().Be("test-utf8");
    }

    [Fact]
    public void ReadCorrelation_PartialHeaders_ReturnsAvailableValues()
    {
        // Arrange
        var headers = new Dictionary<string, byte[]>
        {
            ["correlationId"] = Encoding.UTF8.GetBytes("corr-partial"),
            ["tenantId"] = Encoding.UTF8.GetBytes("tenant-partial")
        };

        // Act
        var result = KafkaHeaders.ReadCorrelation(headers);

        // Assert
        result.CorrelationId.Should().Be("corr-partial");
        result.CausationId.Should().BeNull();
        result.TraceParent.Should().BeNull();
        result.TenantId.Should().Be("tenant-partial");
        result.UserId.Should().BeNull();
    }

    #region Helpers

    private sealed class FakeCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? TraceParent { get; set; }
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
