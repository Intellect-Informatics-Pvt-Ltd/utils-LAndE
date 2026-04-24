using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core.Enrichers;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core.Enrichers;

/// <summary>
/// Property-based test: Canonical field set stability — for any log entry emitted through
/// the platform enrichers, the output contains the expected canonical fields.
/// **Validates: Requirements 2.5**
/// </summary>
public class CanonicalFieldSetPropertyTests
{
    /// <summary>
    /// Property 14: For any log entry enriched by all platform enrichers, the canonical
    /// fields (machine, module, correlationId, log.schema, env) are present.
    /// **Validates: Requirements 2.5**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CanonicalFieldArbitrary) }, MaxTest = 50)]
    public Property AllCanonicalFields_PresentAfterEnrichment(CanonicalFieldInput input)
    {
        var evt = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new[] { new TextToken("Test") }),
            Enumerable.Empty<LogEventProperty>());

        var factory = new SimplePropertyFactory();

        // Apply all enrichers
        var correlationAccessor = new TestCorrelationAccessor { CorrelationId = input.CorrelationId };
        var moduleAccessor = new TestModuleAccessor
        {
            ModuleName = input.ModuleName,
            Environment = input.Environment
        };

        new CorrelationEnricher(correlationAccessor).Enrich(evt, factory);
        new ModuleContextEnricher(moduleAccessor).Enrich(evt, factory);
        new MachineEnricher().Enrich(evt, factory);
        new SchemaVersionEnricher().Enrich(evt, factory);

        var hasCorrelationId = evt.Properties.ContainsKey("correlationId");
        var hasModule = evt.Properties.ContainsKey("module");
        var hasMachine = evt.Properties.ContainsKey("machine");
        var hasLogSchema = evt.Properties.ContainsKey("log.schema");
        var hasEnv = evt.Properties.ContainsKey("env");

        return (hasCorrelationId && hasModule && hasMachine && hasLogSchema && hasEnv)
            .Label($"correlationId={hasCorrelationId}, module={hasModule}, machine={hasMachine}, " +
                   $"log.schema={hasLogSchema}, env={hasEnv}");
    }

    public class CanonicalFieldInput
    {
        public string CorrelationId { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public string Environment { get; set; } = "";
    }

    public static class CanonicalFieldArbitrary
    {
        public static Arbitrary<CanonicalFieldInput> InputArb()
        {
            var gen = from corrId in Arb.Default.NonEmptyString().Generator
                      from module in Gen.Elements("Loans", "FAS", "Savings", "Membership", "Voucher")
                      from env in Gen.Elements("Development", "Staging", "Production")
                      select new CanonicalFieldInput
                      {
                          CorrelationId = corrId.Get,
                          ModuleName = module,
                          Environment = env
                      };

            return Arb.From(gen);
        }
    }

    private class TestCorrelationAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? TraceParent { get; set; }
    }

    private class TestModuleAccessor : IModuleContextAccessor
    {
        public string? ModuleName { get; set; }
        public string? ServiceName { get; set; }
        public string? Environment { get; set; }
        public string? Feature { get; set; }
        public string? Operation { get; set; }
    }
}
