using Intellect.Erp.Observability.IntegrationTests.TestWebApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Intellect.Erp.Observability.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory configured for Production environment to test safety guards.
/// </summary>
public class ProductionWebApplicationFactory : WebApplicationFactory<TestProgram>
{
    public InMemoryTestSink LogSink { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        builder.UseEnvironment("Production");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:ApplicationName"] = "IntegrationTestApp",
                ["Observability:ModuleName"] = "TestModule",
                ["Observability:Environment"] = "Production",
                ["Observability:Masking:Enabled"] = "true",
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
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Sink(LogSink)
                .CreateLogger();
        });
    }
}
