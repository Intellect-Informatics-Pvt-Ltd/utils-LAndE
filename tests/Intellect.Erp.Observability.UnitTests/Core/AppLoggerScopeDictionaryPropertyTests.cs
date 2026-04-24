using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core;

/// <summary>
/// Property-based tests for AppLogger.BeginScope dictionary completeness.
/// **Validates: Requirements 2.2**
/// </summary>
public class AppLoggerScopeDictionaryPropertyTests : IDisposable
{
    private readonly List<LogEvent> _capturedEvents = new();
    private readonly Serilog.Core.Logger _serilogLogger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AppLogger<AppLoggerScopeDictionaryPropertyTests> _appLogger;

    public AppLoggerScopeDictionaryPropertyTests()
    {
        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new AppLoggerTests.InMemoryTestSink(_capturedEvents))
            .CreateLogger();

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(_serilogLogger, dispose: false);
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var msLogger = _loggerFactory.CreateLogger<AppLoggerScopeDictionaryPropertyTests>();
        _appLogger = new AppLogger<AppLoggerScopeDictionaryPropertyTests>(msLogger);
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _serilogLogger.Dispose();
    }

    /// <summary>
    /// Property 15: For any dictionary passed to BeginScope, all keys appear in the
    /// LogContext within the scope.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ScopeDictionaryArbitrary) }, MaxTest = 50)]
    public Property AllDictionaryKeys_AppearInLogContext(Dictionary<string, string> scopeDict)
    {
        // Skip empty dictionaries — nothing to verify
        if (scopeDict.Count == 0)
            return true.ToProperty();

        _capturedEvents.Clear();

        var readOnlyDict = scopeDict.ToDictionary(
            kvp => kvp.Key,
            kvp => (object?)kvp.Value) as IReadOnlyDictionary<string, object?>;

        using (_appLogger.BeginScope(readOnlyDict))
        {
            _appLogger.Information("Test log within scope");
        }

        if (_capturedEvents.Count == 0)
            return false.Label("No log event captured");

        var evt = _capturedEvents[0];

        return scopeDict.Keys.All(key => evt.Properties.ContainsKey(key))
            .Label($"Expected all {scopeDict.Count} keys in log properties, " +
                   $"found {scopeDict.Keys.Count(k => evt.Properties.ContainsKey(k))}");
    }

    /// <summary>
    /// Generates dictionaries with valid C# identifier-like keys for Serilog property names.
    /// </summary>
    public static class ScopeDictionaryArbitrary
    {
        public static Arbitrary<Dictionary<string, string>> DictionaryArb()
        {
            var keyGen = Gen.Elements(
                "alpha", "beta", "gamma", "delta", "epsilon",
                "zeta", "eta", "theta", "iota", "kappa",
                "module", "feature", "operation", "traceId", "userId",
                "tenantId", "requestId", "sessionId", "region", "version");

            var valueGen = Arb.Default.NonEmptyString().Generator
                .Select(s => s.Get);

            var dictGen = Gen.ListOf(Gen.Zip(keyGen, valueGen))
                .Select(pairs =>
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var (k, v) in pairs)
                    {
                        dict[k] = v; // last-write-wins for duplicate keys
                    }
                    return dict;
                });

            return Arb.From(dictGen);
        }
    }
}
