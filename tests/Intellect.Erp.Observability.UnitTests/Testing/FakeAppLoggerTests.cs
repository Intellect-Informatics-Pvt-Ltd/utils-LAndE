using FluentAssertions;
using Microsoft.Extensions.Logging;
using Intellect.Erp.Observability.Testing;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Testing;

/// <summary>
/// Unit tests for <see cref="FakeAppLogger{T}"/>.
/// </summary>
public class FakeAppLoggerTests
{
    private readonly FakeAppLogger<FakeAppLoggerTests> _logger = new();

    [Fact]
    public void Debug_CapturesEntry()
    {
        _logger.Debug("Test message {Value}", 42);

        _logger.Entries.Should().HaveCount(1);
        _logger.Entries[0].Level.Should().Be(LogLevel.Debug);
        _logger.Entries[0].Message.Should().Be("Test message {Value}");
        _logger.Entries[0].Args.Should().ContainSingle().Which.Should().Be(42);
    }

    [Fact]
    public void Debug_WithException_CapturesEntry()
    {
        var ex = new InvalidOperationException("boom");
        _logger.Debug(ex, "Error occurred");

        _logger.Entries.Should().HaveCount(1);
        _logger.Entries[0].Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void Information_CapturesEntry()
    {
        _logger.Information("Info message");

        _logger.HasLoggedAtLevel(LogLevel.Information).Should().BeTrue();
    }

    [Fact]
    public void Warning_CapturesEntry()
    {
        _logger.Warning("Warn message");

        _logger.HasLoggedAtLevel(LogLevel.Warning).Should().BeTrue();
    }

    [Fact]
    public void Error_CapturesEntry()
    {
        _logger.Error("Error message");

        _logger.HasLoggedAtLevel(LogLevel.Error).Should().BeTrue();
    }

    [Fact]
    public void Critical_CapturesEntry()
    {
        _logger.Critical("Critical message");

        _logger.HasLoggedAtLevel(LogLevel.Critical).Should().BeTrue();
    }

    [Fact]
    public void HasLoggedMessage_FindsSubstring()
    {
        _logger.Information("Order {OrderId} processed successfully", "ORD-123");

        _logger.HasLoggedMessage("processed successfully").Should().BeTrue();
        _logger.HasLoggedMessage("nonexistent").Should().BeFalse();
    }

    [Fact]
    public void HasLoggedMessageAtLevel_FiltersCorrectly()
    {
        _logger.Information("Info message");
        _logger.Error("Error message");

        _logger.HasLoggedMessageAtLevel(LogLevel.Information, "Info").Should().BeTrue();
        _logger.HasLoggedMessageAtLevel(LogLevel.Error, "Info").Should().BeFalse();
    }

    [Fact]
    public void Checkpoint_CapturesCheckpointEntry()
    {
        var data = new Dictionary<string, object?> { ["amount"] = 1000m };
        _logger.Checkpoint("PaymentInitiated", data);

        _logger.HasCheckpoint("PaymentInitiated").Should().BeTrue();
        _logger.HasCheckpoint("NonExistent").Should().BeFalse();

        var entry = _logger.Entries[0];
        entry.Level.Should().Be(LogLevel.Information);
        entry.Checkpoint.Should().Be("PaymentInitiated");
        entry.CheckpointData.Should().ContainKey("amount");
    }

    [Fact]
    public void HasLoggedException_DetectsExceptionType()
    {
        _logger.Error(new ArgumentNullException("param"), "Null argument");

        _logger.HasLoggedException<ArgumentNullException>().Should().BeTrue();
        _logger.HasLoggedException<InvalidOperationException>().Should().BeFalse();
        _logger.HasLoggedException().Should().BeTrue();
    }

    [Fact]
    public void BeginScope_CapturesScopeData()
    {
        var scope = new Dictionary<string, object?> { ["tenantId"] = "T001" };
        using (_logger.BeginScope(scope))
        {
            _logger.Information("Scoped message");
        }

        _logger.Entries[0].ScopeData.Should().ContainKey("tenantId");
        _logger.Entries[0].ScopeData!["tenantId"].Should().Be("T001");
    }

    [Fact]
    public void BeginOperation_CapturesOperationScope()
    {
        using (_logger.BeginOperation("Loans", "Disbursement", "Create"))
        {
            _logger.Information("Operation message");
        }

        var scopeData = _logger.Entries[0].ScopeData;
        scopeData.Should().ContainKey("module").WhoseValue.Should().Be("Loans");
        scopeData.Should().ContainKey("feature").WhoseValue.Should().Be("Disbursement");
        scopeData.Should().ContainKey("operation").WhoseValue.Should().Be("Create");
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _logger.Information("Message 1");
        _logger.Error("Message 2");

        _logger.Clear();

        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Entries_PreservesOrder()
    {
        _logger.Debug("First");
        _logger.Information("Second");
        _logger.Warning("Third");

        _logger.Entries.Should().HaveCount(3);
        _logger.Entries[0].Message.Should().Be("First");
        _logger.Entries[1].Message.Should().Be("Second");
        _logger.Entries[2].Message.Should().Be("Third");
    }
}
