# utils-LAndE — Observability & Error Handling Platform

A family of .NET 8 NuGet packages that standardize structured logging, correlation propagation, global exception handling, error cataloging, sensitive-field masking, and audit-hook extensibility for the ePACS ERP system.

## Packages

| Package | Description |
|---------|-------------|
| `Intellect.Erp.Observability.Abstractions` | Public contracts (interfaces, records, enums, attributes) with zero implementation dependencies |
| `Intellect.Erp.ErrorHandling` | Typed exception hierarchy, error factory, YAML error catalog loader |
| `Intellect.Erp.Observability.Core` | AppLogger, Serilog enrichers, redaction engine, configuration, DI extensions |
| `Intellect.Erp.Observability.AspNetCore` | Middlewares (correlation, exception handling, context enrichment, request logging), action filters, HttpContext-backed accessors |
| `Intellect.Erp.Observability.Propagation` | Outbound HTTP correlation handler, Kafka header helpers, traceable background service |
| `Intellect.Erp.Observability.AuditHooks` | Audit event hooks (LogOnly, TraceabilityBridge, Kafka) |
| `Intellect.Erp.Observability.Log4NetBridge` | SerilogForwardingAppender for legacy log4net modules |
| `Intellect.Erp.Observability.Integrations.Traceability` | Adapter shim for the Traceability sibling utility |
| `Intellect.Erp.Observability.Integrations.Messaging` | Kafka envelope enricher for the Messaging sibling utility |
| `Intellect.Erp.Observability.Testing` | Fakes, in-memory sinks, and assertion helpers for testing |

## Quick Start

### 1. Add References

```xml
<ItemGroup>
  <PackageReference Include="Intellect.Erp.Observability.AspNetCore" />
  <PackageReference Include="Intellect.Erp.ErrorHandling" />
</ItemGroup>
```

### 2. Configure `appsettings.json`

```json
{
  "Observability": {
    "ApplicationName": "epacs-loans",
    "ModuleName": "Loans",
    "Environment": "Development",
    "Sinks": {
      "Console": { "Enabled": true },
      "Elasticsearch": { "Enabled": true, "Url": "http://elk:9200" }
    },
    "Masking": { "Enabled": true }
  },
  "ErrorHandling": {
    "ReturnProblemDetails": true,
    "CatalogFiles": ["config/error-catalog/core.yaml", "config/error-catalog/loans.yaml"],
    "ClientErrorUriBase": "https://errors.epacs.in/"
  }
}
```

### 3. Wire Up in `Program.cs`

```csharp
using Intellect.Erp.Observability.AspNetCore;
using Intellect.Erp.Observability.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseObservability();                              // Serilog bootstrap
builder.Services.AddObservability(builder.Configuration);     // IAppLogger<T>, enrichers, masking
builder.Services.AddObservabilityAccessors();                 // HttpContext-backed accessors
builder.Services.AddErrorHandling(builder.Configuration);     // IErrorFactory, error catalog

builder.Services.AddControllers();
var app = builder.Build();

app.UseObservability();   // Correlation → GlobalException → ContextEnrichment → RequestLogging
app.UseRouting();
app.MapControllers();
app.Run();
```

### 4. Annotate and Throw

```csharp
[HttpPost("disburse")]
[BusinessOperation("Loans", "Disbursement", "Create")]
public IActionResult Disburse(LoanRequest request)
{
    if (request.Amount <= 0)
        throw _errorFactory.Validation("Amount must be > 0",
            [new FieldError("Amount", "amount-positive", "Must be > 0")]);

    _logger.Checkpoint("DisbursementAccepted", new Dictionary<string, object?>
    {
        ["MemberId"] = request.MemberId,
        ["Amount"] = request.Amount,
    });

    return Ok(new { success = true });
}
```

### 5. PII Masking on DTOs

```csharp
public sealed class LoanRequest
{
    public string MemberId { get; init; } = default!;
    [Sensitive(keepLast: 4)] public string? AadhaarNumber { get; init; }
    [DoNotLog]               public string? AttachmentBase64 { get; init; }
    [Mask(@"\d{6,}")]        public string? AccountNumber { get; init; }
}
```

## Middleware Pipeline

`UseObservability()` registers middlewares in this order:

1. **CorrelationMiddleware** — generates/reads correlation ID, pushes to LogContext, echoes on response
2. **GlobalExceptionMiddleware** — catches exceptions, maps to ErrorResponse JSON
3. **ContextEnrichmentMiddleware** — populates user/tenant/PACS context from claims (post-auth)
4. **RequestLoggingMiddleware** — logs request start/end with duration and status

## Error Response Format

```json
{
  "success": false,
  "errorCode": "ERP-LOANS-VAL-0001",
  "title": "Invalid loan amount",
  "message": "Loan amount must be greater than zero.",
  "correlationId": "01HZX5PQ8WZ4A5C2M8YT4F3M6V",
  "status": 400,
  "severity": "warning",
  "retryable": false,
  "type": "https://errors.epacs.in/ERP-LOANS-VAL-0001",
  "timestamp": "2024-06-15T10:23:45.123Z"
}
```

## Documentation

- [Adoption Guide](docs/adoption-guide.md) — step-by-step setup for consuming modules
- [Error Catalog Authoring](docs/error-catalog-authoring.md) — creating per-module YAML error catalogs
- [ELK Field Reference](docs/elk-field-reference.md) — canonical field set for Kibana dashboards
- [Log4Net Migration](docs/migration-from-log4net.md) — bridging legacy log4net output
- [Adoption Quick Reference](docs/adoption-quickref.md) — one-page cheat sheet

## Project Structure

```
utils-LAndE/
├── src/                          # Source packages
│   ├── Intellect.Erp.Observability.Abstractions/
│   ├── Intellect.Erp.ErrorHandling/
│   ├── Intellect.Erp.Observability.Core/
│   ├── Intellect.Erp.Observability.AspNetCore/
│   ├── Intellect.Erp.Observability.Propagation/
│   ├── Intellect.Erp.Observability.AuditHooks/
│   ├── Intellect.Erp.Observability.Log4NetBridge/
│   ├── Intellect.Erp.Observability.Integrations.Traceability/
│   ├── Intellect.Erp.Observability.Integrations.Messaging/
│   └── Intellect.Erp.Observability.Testing/
├── tests/                        # Test projects
│   ├── Intellect.Erp.Observability.UnitTests/
│   ├── Intellect.Erp.Observability.IntegrationTests/
│   └── Intellect.Erp.ErrorHandling.UnitTests/
├── samples/SampleHost/           # Minimal adoption example
├── config/error-catalog/         # YAML error catalog files
└── docs/                         # Documentation
```

## Building

```bash
dotnet build
dotnet test
```

## License

Proprietary — Intellect Design Arena Ltd.
