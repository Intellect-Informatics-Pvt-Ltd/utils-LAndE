using Intellect.Erp.Observability.AspNetCore;
using Intellect.Erp.Observability.AspNetCore.Filters;
using Intellect.Erp.Observability.Core;
using Intellect.Erp.ErrorHandling.Exceptions;
using Intellect.Erp.Observability.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Intellect.Erp.Observability.IntegrationTests.TestWebApp;

/// <summary>
/// Minimal test web application used by WebApplicationFactory-based integration tests.
/// Registers observability middleware and services, and exposes test endpoints.
/// </summary>
public class TestProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Directory.GetCurrentDirectory()
        });
        ConfigureServices(builder);
        var app = builder.Build();
        ConfigurePipeline(app);
        app.Run();
    }

    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Configure Serilog with LogContext enrichment
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add in-memory configuration for Observability and ErrorHandling
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
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

        builder.Services.AddObservability(builder.Configuration);
        builder.Services.AddObservabilityAccessors();
        builder.Services.AddErrorHandling(builder.Configuration);

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<BusinessOperationFilter>();
            options.Filters.Add<ValidationResultFilter>();
        });
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseObservability();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Simple test endpoints (non-controller)
        app.MapGet("/test/ok", () => Results.Ok(new { message = "OK" }));

        app.MapGet("/test/throw/validation", () =>
        {
            throw new ValidationException(
                "Validation failed",
                new[] { new FieldError("Name", "REQUIRED", "Name is required") });
        });

        app.MapGet("/test/throw/business-rule", () =>
        {
            throw new BusinessRuleException("Business rule violated");
        });

        app.MapGet("/test/throw/not-found", () =>
        {
            throw new NotFoundException("Resource not found");
        });

        app.MapGet("/test/throw/conflict", () =>
        {
            throw new ConflictException("Resource conflict");
        });

        app.MapGet("/test/throw/unauthorized", () =>
        {
            throw new UnauthorizedException("Not authenticated");
        });

        app.MapGet("/test/throw/forbidden", () =>
        {
            throw new ForbiddenException("Access denied");
        });

        app.MapGet("/test/throw/concurrency", () =>
        {
            throw new ConcurrencyException("Concurrency conflict");
        });

        app.MapGet("/test/throw/data-integrity", () =>
        {
            throw new DataIntegrityException("Data integrity error");
        });

        app.MapGet("/test/throw/integration", () =>
        {
            throw new IntegrationException("Integration failed", retryable: true);
        });

        app.MapGet("/test/throw/dependency", () =>
        {
            throw new DependencyException("Dependency unavailable");
        });

        app.MapGet("/test/throw/external-system", () =>
        {
            throw new ExternalSystemException("External system error");
        });

        app.MapGet("/test/throw/system", () =>
        {
            throw new ErrorHandling.Exceptions.SystemException("System error");
        });

        app.MapGet("/test/throw/task-canceled", (HttpContext ctx) =>
        {
            throw new TaskCanceledException("Request was cancelled");
#pragma warning disable CS0162
            return Results.Ok();
#pragma warning restore CS0162
        });

        app.MapGet("/test/throw/unknown", () =>
        {
            throw new InvalidOperationException("Something unexpected happened");
        });

        app.MapGet("/test/slow", async () =>
        {
            await Task.Delay(100);
            return Results.Ok(new { message = "slow" });
        });

        app.MapGet("/health", () => Results.Ok("healthy"));
        app.MapGet("/metrics", () => Results.Ok("metrics"));
    }
}
