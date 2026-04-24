using Intellect.Erp.Observability.IntegrationTests.TestWebApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Intellect.Erp.Observability.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory that configures the test web application
/// with observability middleware and services.
/// </summary>
public class ObservabilityWebApplicationFactory : WebApplicationFactory<TestProgram>
{
    /// <summary>
    /// Captured log sink for verifying log output in tests.
    /// </summary>
    public InMemoryTestSink LogSink { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:ApplicationName"] = "IntegrationTestApp",
                ["Observability:ModuleName"] = "TestModule",
                ["Observability:Environment"] = "Development",
                ["Observability:RequestLogging:SlowRequestThresholdMs"] = "50",
                ["Observability:RequestLogging:ExcludePaths:0"] = "/health",
                ["Observability:RequestLogging:ExcludePaths:1"] = "/metrics",
                ["ErrorHandling:DefaultErrorCode"] = "ERP-CORE-SYS-0001",
                ["ErrorHandling:ClientErrorUriBase"] = "https://errors.epacs.in/",
                ["ErrorHandling:IncludeExceptionDetailsInResponse"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Set the static Log.Logger to use our test sink
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Sink(LogSink)
                .CreateLogger();
        });
    }
}

/// <summary>
/// In-memory Serilog sink that captures log events for test assertions.
/// </summary>
public class InMemoryTestSink : Serilog.Core.ILogEventSink
{
    private readonly List<LogEvent> _events = new();
    private readonly object _lock = new();

    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_lock)
            {
                return _events.ToList();
            }
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_lock)
        {
            _events.Add(logEvent);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
