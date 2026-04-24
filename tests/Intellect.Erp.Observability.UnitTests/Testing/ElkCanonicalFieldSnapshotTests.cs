using System.Text.Json;
using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core.Enrichers;
using Intellect.Erp.Observability.Testing;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Testing;

/// <summary>
/// Golden-file snapshot tests for the ELK JSON canonical field set (schema v1).
/// Ensures that the canonical field names do not change unintentionally across versions.
/// **Validates: Requirements 2.5, 18.4**
/// </summary>
public class ElkCanonicalFieldSnapshotTests
{
    private static readonly string[] ExpectedCanonicalFields =
    [
        "@timestamp",
        "level",
        "app",
        "env",
        "machine",
        "module",
        "correlationId",
        "log.schema"
    ];

    [Fact]
    public void GoldenFile_ContainsExpectedCanonicalFields()
    {
        // Arrange — load the golden file
        var goldenFilePath = Path.Combine(
            AppContext.BaseDirectory, "Testing", "elk-canonical-fields-v1.golden.json");

        File.Exists(goldenFilePath).Should().BeTrue(
            "the golden file should be present at {0}", goldenFilePath);

        var json = File.ReadAllText(goldenFilePath);
        var doc = JsonDocument.Parse(json);
        var fieldsElement = doc.RootElement.GetProperty("fields");
        var goldenFields = fieldsElement.EnumerateArray()
            .Select(e => e.GetString()!)
            .ToArray();

        // Assert — golden file matches expected canonical fields exactly
        goldenFields.Should().BeEquivalentTo(ExpectedCanonicalFields,
            "the golden file must contain exactly the canonical field set v1");
    }

    [Fact]
    public void FullPipeline_EmitsAllCanonicalFields()
    {
        // Arrange — set up an in-memory Serilog pipeline with all enrichers
        var sink = new InMemoryLogSink();
        var correlationAccessor = new FakeCorrelationContextAccessor
        {
            CorrelationId = "01HXYZ1234567890ABCDEFGHIJ"
        };
        var moduleAccessor = new FakeModuleContextAccessor
        {
            ModuleName = "Loans",
            ServiceName = "LoanService",
            Environment = "Development"
        };

        using var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.With(new CorrelationEnricher(correlationAccessor))
            .Enrich.With(new ModuleContextEnricher(moduleAccessor))
            .Enrich.With(new MachineEnricher())
            .Enrich.With(new SchemaVersionEnricher())
            .Enrich.WithProperty("app", "TestApp")
            .WriteTo.Sink(sink)
            .CreateLogger();

        // Act — emit a log entry through the full pipeline
        logger.Information("Test canonical field emission");

        // Assert — verify all canonical fields are present
        sink.Events.Should().HaveCount(1);
        var logEvent = sink.Events[0];

        // @timestamp is always present on LogEvent (logEvent.Timestamp)
        logEvent.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // level is always present on LogEvent (logEvent.Level)
        logEvent.Level.Should().Be(LogEventLevel.Information);

        // Enriched properties
        logEvent.Properties.Should().ContainKey("app", "canonical field 'app' must be present");
        logEvent.Properties.Should().ContainKey("env", "canonical field 'env' must be present");
        logEvent.Properties.Should().ContainKey("machine", "canonical field 'machine' must be present");
        logEvent.Properties.Should().ContainKey("module", "canonical field 'module' must be present");
        logEvent.Properties.Should().ContainKey("correlationId", "canonical field 'correlationId' must be present");
        logEvent.Properties.Should().ContainKey("log.schema", "canonical field 'log.schema' must be present");

        // Verify values
        GetScalarValue(logEvent, "log.schema").Should().Be("v1");
        GetScalarValue(logEvent, "correlationId").Should().Be("01HXYZ1234567890ABCDEFGHIJ");
        GetScalarValue(logEvent, "module").Should().Be("Loans");
        GetScalarValue(logEvent, "env").Should().Be("Development");
        GetScalarValue(logEvent, "app").Should().Be("TestApp");
    }

    [Fact]
    public void GoldenFile_FieldNames_MatchEnricherPropertyNames()
    {
        // This test ensures the golden file field names match what the enrichers actually produce.
        // If an enricher changes a property name, this test will catch the drift.

        var goldenFilePath = Path.Combine(
            AppContext.BaseDirectory, "Testing", "elk-canonical-fields-v1.golden.json");
        var json = File.ReadAllText(goldenFilePath);
        var doc = JsonDocument.Parse(json);
        var goldenFields = doc.RootElement.GetProperty("fields")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToHashSet();

        // Emit a log through enrichers and collect property names
        var sink = new InMemoryLogSink();
        var correlationAccessor = new FakeCorrelationContextAccessor { CorrelationId = "test-id" };
        var moduleAccessor = new FakeModuleContextAccessor
        {
            ModuleName = "TestModule",
            Environment = "Test"
        };

        using var logger = new LoggerConfiguration()
            .Enrich.With(new CorrelationEnricher(correlationAccessor))
            .Enrich.With(new ModuleContextEnricher(moduleAccessor))
            .Enrich.With(new MachineEnricher())
            .Enrich.With(new SchemaVersionEnricher())
            .Enrich.WithProperty("app", "TestApp")
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Snapshot test");

        var emittedPropertyNames = sink.Events[0].Properties.Keys.ToHashSet();

        // The golden file fields that are enricher-produced properties
        // (@timestamp and level are intrinsic to LogEvent, not properties)
        var enricherFields = goldenFields
            .Where(f => f != "@timestamp" && f != "level")
            .ToList();

        foreach (var field in enricherFields)
        {
            emittedPropertyNames.Should().Contain(field,
                "golden file field '{0}' must be produced by an enricher", field);
        }
    }

    private static string? GetScalarValue(LogEvent logEvent, string propertyName)
    {
        if (logEvent.Properties.TryGetValue(propertyName, out var value) && value is ScalarValue scalar)
        {
            return scalar.Value?.ToString();
        }
        return null;
    }
}
