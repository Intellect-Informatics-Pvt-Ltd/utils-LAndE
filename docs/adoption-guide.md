# Adoption Guide

Step-by-step guide for adopting the Observability Platform in an ePACS module.

## Prerequisites

- .NET 8.0 SDK
- An ASP.NET Core Web API project

## 1. Add NuGet References

Add project references (or NuGet package references once published) to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Intellect.Erp.Observability.AspNetCore\Intellect.Erp.Observability.AspNetCore.csproj" />
  <ProjectReference Include="..\..\src\Intellect.Erp.ErrorHandling\Intellect.Erp.ErrorHandling.csproj" />
</ItemGroup>
```

When packages are published to NuGet:

```xml
<ItemGroup>
  <PackageReference Include="Intellect.Erp.Observability.AspNetCore" />
  <PackageReference Include="Intellect.Erp.ErrorHandling" />
</ItemGroup>
```

The `AspNetCore` package transitively brings in `Observability.Core` and `Observability.Abstractions`.

## 2. Configure `appsettings.json`

Add the `Observability` and `ErrorHandling` sections to your `appsettings.json`:

```json
{
  "Observability": {
    "ApplicationName": "epacs-loans",
    "ModuleName": "Loans",
    "Environment": "Development",
    "Sinks": {
      "Console": { "Enabled": true, "CompactFormat": false },
      "File": { "Enabled": true, "Path": "logs/app-.log", "RollingInterval": "Day", "JsonFormat": true },
      "Elasticsearch": { "Enabled": false, "Url": "http://elk:9200" }
    },
    "Masking": {
      "Enabled": true,
      "Paths": ["$.body.password", "$.headers.authorization"]
    },
    "RequestLogging": {
      "SlowRequestThresholdMs": 3000,
      "ExcludePaths": ["/health", "/metrics", "/swagger"]
    }
  },
  "ErrorHandling": {
    "IncludeExceptionDetailsInResponse": false,
    "ReturnProblemDetails": true,
    "DefaultErrorCode": "ERP-CORE-SYS-0001",
    "CatalogFiles": [
      "config/error-catalog/core.yaml",
      "config/error-catalog/loans.yaml"
    ],
    "ClientErrorUriBase": "https://errors.epacs.in/"
  }
}
```

Key settings:
- `ApplicationName` and `ModuleName` are required and validated at startup.
- `Masking.Enabled` is enforced to `true` in Production environments.
- `IncludeExceptionDetailsInResponse` is refused in Production.
- `CatalogFiles` lists the YAML error catalog files to load.

## 3. Add the Three One-Line Calls in `Program.cs`

```csharp
using Intellect.Erp.Observability.AspNetCore;
using Intellect.Erp.Observability.Core;

var builder = WebApplication.CreateBuilder(args);

// 1. Bootstrap Serilog from Observability config
builder.Host.UseObservability();

// 2. Register observability services (IAppLogger<T>, IRedactionEngine, enrichers)
builder.Services.AddObservability(builder.Configuration);

// 3. Register HttpContext-backed context accessors
builder.Services.AddObservabilityAccessors();

// 4. Register error handling (IErrorFactory, IErrorCatalog)
builder.Services.AddErrorHandling(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

// 5. Register the middleware pipeline
//    Correlation → GlobalException → ContextEnrichment → RequestLogging
app.UseObservability();

app.UseRouting();
app.MapControllers();
app.Run();
```

That's it. Five calls replace ~80 lines of inline middleware.

## 4. Annotate Controllers

Use `[BusinessOperation]` to declare the module, feature, and operation context. The `BusinessOperationFilter` automatically pushes these into the log scope.

```csharp
[ApiController]
[Route("api/[controller]")]
public class LoanController : ControllerBase
{
    private readonly IAppLogger<LoanController> _logger;
    private readonly IErrorFactory _errorFactory;

    public LoanController(IAppLogger<LoanController> logger, IErrorFactory errorFactory)
    {
        _logger = logger;
        _errorFactory = errorFactory;
    }

    [HttpPost("disburse")]
    [BusinessOperation("Loans", "Disbursement", "Create")]
    public IActionResult Disburse(LoanDisbursementRequest request)
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
}
```

## 5. Annotate DTOs for PII Masking

Use `[Sensitive]`, `[DoNotLog]`, and `[Mask]` attributes on DTO properties. The `IRedactionEngine` automatically masks these before they reach any log sink.

```csharp
public sealed class LoanDisbursementRequest
{
    public string MemberId { get; init; } = default!;
    public decimal Amount { get; init; }

    [Sensitive(keepLast: 4)]
    public string? AadhaarNumber { get; init; }

    [DoNotLog]
    public string? AttachmentBase64 { get; init; }

    [Mask(@"\d{6,}", "***")]
    public string? AccountNumber { get; init; }
}
```

Attribute behavior:
- `[Sensitive(keepLast: N)]` — masks all characters except the last N (e.g., `********1234`)
- `[DoNotLog]` — completely excludes the field from log output
- `[Mask(regex, replacement)]` — applies a regex pattern and replaces matches

## 6. Throw Typed Exceptions

Inject `IErrorFactory` and throw typed exceptions. The `GlobalExceptionMiddleware` catches them and returns a consistent `ErrorResponse` JSON body.

```csharp
// Validation (HTTP 400)
throw _errorFactory.Validation("Invalid input", [new FieldError("Email", "email-format", "Invalid email")]);

// Not found (HTTP 404)
throw _errorFactory.NotFound("Member not found.");

// Business rule (HTTP 422)
throw _errorFactory.BusinessRule("Member is not eligible.");

// Conflict (HTTP 409)
throw _errorFactory.Conflict("Loan already approved.");

// From catalog (uses YAML-defined error)
throw _errorFactory.FromCatalog("ERP-LOANS-VAL-0001");
```

## 7. Create Your Error Catalog

Create a YAML file for your module's errors (see [Error Catalog Authoring Guide](error-catalog-authoring.md)):

```yaml
errors:
  - code: "ERP-LOANS-VAL-0001"
    title: "Invalid loan amount"
    userMessage: "Loan amount must be greater than zero."
    supportMessage: "Client submitted amount <= 0."
    httpStatus: 400
    severity: "Warning"
    retryable: false
    category: "Validation"
```

Add the file path to `ErrorHandling:CatalogFiles` in `appsettings.json`.

## 8. Use Structured Logging

The `IAppLogger<T>` provides structured logging with automatic context enrichment:

```csharp
// Standard log levels
_logger.Information("Processing loan {LoanId} for member {MemberId}", loanId, memberId);
_logger.Warning("Slow query detected: {DurationMs}ms", duration);

// Scoped operations
using (_logger.BeginOperation("Loans", "Disbursement", "Create"))
{
    _logger.Information("Starting disbursement");
    // All logs within this scope include module/feature/operation fields
}

// Business checkpoints
_logger.Checkpoint("PaymentInitiated", new Dictionary<string, object?>
{
    ["Amount"] = amount,
    ["PaymentRef"] = paymentRef,
});
```

Every log entry automatically includes the canonical field set: `@timestamp`, `level`, `app`, `env`, `machine`, `module`, `correlationId`, and `log.schema`.

## Next Steps

- [Error Catalog Authoring Guide](error-catalog-authoring.md) — how to create per-module YAML catalogs
- [ELK Field Reference](elk-field-reference.md) — canonical field set for Kibana dashboards
- [Log4Net Migration Guide](migration-from-log4net.md) — bridging legacy log4net output
