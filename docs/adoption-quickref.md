# utils-LAndE — One-page adoption quick reference

Full plan: `thoughts/shared/plans/utils-LAndE-implementation-plan.md`

## Program.cs (replaces ~80 lines of inline middleware across every module today)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseObservability();                               // Serilog from config
builder.Services.AddObservability(builder.Configuration);      // IAppLogger<T>, accessors, masking, catalog
builder.Services.AddErrorHandling(builder.Configuration);      // IErrorFactory + catalog load
builder.Services.AddHttpClient("core").AddObservabilityCorrelation();

var app = builder.Build();
app.UseObservability();   // Correlation → GlobalException → (auth) → ContextEnrichment → RequestLogging
app.MapControllers();
app.Run();
```

## Typical controller

```csharp
[HttpPost]
[BusinessOperation("Loans", "Application", "Submit")]
public async Task<IActionResult> Submit(LoanApplicationRequest dto, CancellationToken ct)
    => Ok(await _service.SubmitAsync(dto, ct));
```

## Typical service (no more catch/log/rethrow)

```csharp
public async Task<LoanApplicationId> SubmitAsync(LoanApplicationRequest dto, CancellationToken ct)
{
    if (dto.Amount <= 0)
        throw _errorFactory.Validation("ERP-LOANS-VAL-0001", "Loan amount must be greater than zero.",
            new FieldError("Amount", "amount-positive", "Must be > 0"));

    var member = await _members.FindAsync(dto.MemberId, ct)
        ?? throw _errorFactory.NotFound("ERP-LOANS-NFD-0003", "Member not found.");

    if (member.IsBlacklisted)
        throw _errorFactory.Business("ERP-LOANS-BIZ-0012", "Member is not eligible for a new loan.");

    _logger.Checkpoint("LoanApplicationAccepted", new Dictionary<string, object?>
    {
        ["MemberId"] = member.Id,
        ["Amount"] = dto.Amount,
    });

    return await _repo.InsertAsync(dto, ct);
}
```

## DTO with PII

```csharp
public sealed class LoanApplicationRequest
{
    public string MemberId { get; init; } = default!;
    [Sensitive(keepLast: 4)] public string AadhaarNumber { get; init; } = default!;
    [DoNotLog]               public string AttachmentBase64 { get; init; } = default!;
    [Mask("\\d{6,}")]        public string AccountNumber { get; init; } = default!;
}
```

## appsettings.json essentials

```json
{
  "Observability": {
    "ApplicationName": "epacs-loans",
    "ModuleName": "Loans",
    "Environment": "Prod",
    "DefaultMinimumLevel": "Information",
    "Sinks": { "Elasticsearch": { "Enabled": true, "Url": "http://elk:9200" } }
  },
  "ErrorHandling": {
    "IncludeExceptionDetailsInResponse": false,
    "ReturnProblemDetails": true,
    "CatalogFiles": ["config/error-catalog/core.yaml","config/error-catalog/loans.yaml"]
  }
}
```

## Standard error response (BRD §14)

```json
{
  "success": false,
  "errorCode": "ERP-LOANS-VAL-0001",
  "title": "Validation failed",
  "message": "Loan amount must be greater than zero.",
  "correlationId": "01HZX5PQ8WZ4A5C2M8YT4F3M6V",
  "status": 400,
  "severity": "warning",
  "retryable": false,
  "fieldErrors": [{ "field": "Amount", "code": "amount-positive", "message": "Must be > 0" }]
}
```
